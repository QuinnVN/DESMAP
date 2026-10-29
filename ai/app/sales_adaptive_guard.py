"""Adaptive-only output guard for ``lan-adaptive-v2``.

The legacy writer guard (``sales_openrouter.customer_text_rejections``) is
called unchanged. v2 keeps its grounding codes, re-checks the codes that were
keyword-only (policy, refund, receipt, completion, physical action, periods,
facts) clause by clause, and downgrades style codes to warnings.

Clause rules (conservative regex heuristics, not a semantic verifier):
- A match is *attributed* when a reported-speech marker ("em nói", "em bảo",
  "em hứa"...) precedes it in the same clause, no contrastive break ("thì",
  "nhưng", "mà") separates them, and the clause does not endorse it
  ("em nói đúng", "như em nói"). A balanced quote immediately followed by
  "em vừa nói vậy" is also attributed. Quotation marks alone attribute nothing.
- A clause is a *question* when it ends a "?" sentence or contains a wh-word.
  A yes/no question exempts a match only when it questions that content
  ("em đã đổi chưa?", "em có hoàn tiền không?", "... phải không?"); one that
  wraps an assertion ("cửa hàng đã hoàn tiền rồi, em biết không?") does not.
  wh-questions may not presuppose a completed claim ("sao cửa hàng đã đồng ý
  hoàn tiền...").
- A match is *denied* when exactly one negator governs it: same clause, no
  break ("vì", "mà", "nhưng", "thì"...) between, and no tense or approval
  marker of another predicate between ("chị không biết em đã hoàn tiền" is not
  a denial). A trailing refusal ("... là sai", "... chị không cần") also
  denies; "không sai" does not. "Có quyền ... đâu" denies authority and
  "chị không nghĩ cửa hàng sẽ ..." negates a future expectation. A customer's
  double-negative request is still a request, never store authorization.
- Every other clause is Lan's own assertion. An asserted unauthorized claim is
  blocked even if an earlier clause in the same text was negated or quoted.

Lan's own *request* for a refund, discount, compensation or guarantee is not a
store authorization. It is accepted with the warning
``customer_requests_unauthorized_remedy`` unless the same clause also claims a
store approval or that Lan received it.
"""
from __future__ import annotations

import re
from dataclasses import dataclass
from typing import Any, Mapping

from app.sales_openrouter import (FACTS, SCENARIO, _FACT_ASSERTIONS, _FACT_CONTRADICTIONS, _FACT_MARKERS, _fact_ids, _asserted_fact,
                                  _normalize, _period_claims, _required_content_present, _valid_period,
                                  customer_text_rejections, fact_texts)

# Legacy codes kept as hard blocks. ``missing_required_content`` only carries
# code-owned content here (the trust challenge); ending tones are checked below.
LEGACY_HARD = frozenset({"missing_text", "invalid_format", "unsupported_detail", "missing_required_fact",
                         "missing_required_content", "false_hearing_failure", "false_already_answered",
                         "false_missing_information"})
# Legacy style codes reported as warnings only: echo/paraphrase, repetition and
# goodbye keywords while continuing.
LEGACY_WARNINGS = frozenset({"repeated_reply", "transcript_echo", "administrative_tone", "unplanned_ending"})
# Legacy codes v2 recomputes below (assertion-aware or diacritic-aware):
# policy_or_instruction, wrong_address, unallowed_fact, contradicted_fact,
# unsupported_completion, physical_action, invented_policy,
# wrong_purchase_period, too_long, too_many_sentences.
HARD_MAX_CHARS = 600
HARD_MAX_WORDS = 80
SOFT_MAX_WORDS = 55
# More sentences than this is a style warning, never a rejection.
SOFT_MAX_SENTENCES = 2

_INSTRUCTION_LEAK = ("rubric", "diem so", "cham diem", "qwen", "jev", "openrouter", "dap an", "nguoi choi",
                     "ban can noi", "em hay doc", "prompt", "json", "tri tue nhan tao", "mo hinh ngon ngu",
                     "tro ly ao", "nhap vai", "kich ban", "tro choi", "ung dung", "huong dan he thong", "system")
_KEY_LEAK = re.compile(r"\b(?:allowedActions|factIds|customerText|closeReason|mustAnswerFactIds|hintLevel|openConcern|"
                       r"recognizedActs|storePolicy|deterministicEnding|endingTone|actionMemory|closeAllowed)\b")
_WRONG_ADDRESS = re.compile(r"(?<!\w)(?:tôi|bạn|quý khách)(?!\w)")

# ------------------------------------------------------------------ clauses

_QUOTE = re.compile(r"(?<!\w)(?:(?:em|bên em|cửa hàng|quản lý|nhân viên|shop|tiệm)\s+(?:(?:vừa|cứ|lại|đã|mới|thì|còn|"
                    r"cũng|có)\s+){0,2}(?:nói|bảo|hứa|đòi|bắt|yêu cầu|đề nghị|tuyên bố|khẳng định|nhắc|trả lời|"
                    r"giải thích|cho rằng|kêu|tưởng|bảo là|nói là)|theo lời em|lời em|ý em là|theo em|nghe em nói)(?!\w)")
_ENDORSE = re.compile(r"(?<!\w)(?:đúng rồi|đúng là|nói đúng|như em nói|như em bảo|chị đồng ý|chị chấp nhận|chị hiểu rồi|"
                      r"hợp lý|vậy cũng được|thế cũng được|chị tin)(?!\w)")
_WH = re.compile(r"\b(?:tai sao|vi sao|sao lai|lam sao|the nao|ra sao|(?<!khong )(?<!chua )(?<!chang )bao gio|bao lau|o dau|cai gi|vi gi|de lam gi|"
                 r"gi chu|gi vay|gi a)\b")
_YESNO = re.compile(r"\b(?:co phai|phai khong|duoc khong|hay khong|hay chua)\b")
_CONNECTIVE = re.compile(r",|;|\s(?=(?:nhưng|mà|song|tuy nhiên|thế mà|vậy mà)\s)")
# "đừng" only with a speech verb: bare "dung" also means "đúng"/"dùng".
_NEGATOR = re.compile(r"\b(?:khong|chua|chang|khoi|dau co|dau phai|tu choi|dung (?:hua|noi|bao|cam ket|dam bao|"
                      r"khang dinh))\b(?! chi\b| nhung\b)")
# A negator or a reported-speech marker stops governing at these words.
_DENIAL_BREAK = re.compile(r"\b(?:vi(?! sao| gi)|ma|nhung|va|nen|thi|neu|khi|boi vi|tai vi|trong khi)\b")
_QUOTE_BREAK = re.compile(r"\b(?:thi|nhung|ma)\b")
# Another predicate's tense or a store approval between negator and match.
_OTHER_PREDICATE = re.compile(r"\b(?:da|vua|se|roi|xong)\b|(?:\bem|cua hang|tiem|shop|ben em|quan ly)\s+"
                              r"(?:dong y|chap nhan|duyet|cho phep|quyet dinh)\b")
# Yes/no forms that question the content they follow or wrap.
_COMPLETION_QUESTION = re.compile(r"\b(?:chua|hay chua)\s*[?.!…]*\s*$")
_TAG_QUESTION = re.compile(r"\b(?:phai khong|co phai|dung khong|that khong|that sao|chu|a|ha|sao)\s*[?.!…]*\s*$")
# Only a balanced quotation followed immediately by its speaker is reported
# speech. Bare quotation marks and a later endorsement do not exempt a claim.
_REPORTED_QUOTE = re.compile(r'"[^"\n]+"|“[^”\n]+”')
_POST_QUOTE_SPEAKER = re.compile(r"^\s*[,;:]\s*(?:em|bên em|nhân viên|quản lý)\s+"
                                 r"(?:(?:vừa|đã|mới|cũng)\s+){0,2}(?:nói|bảo|hứa|khẳng định)\s+(?:vậy|thế)\b")
_NEGATIVE_EXPECTATION = re.compile(r"^\s*chi\s+(?:khong|chang)\s+(?:nghi|tin|cho rang)\s+(?:la\s+)?"
                                   r"(?:cua hang|ben em|em|quan ly|shop|tiem)\s+se\s+")


@dataclass(frozen=True)
class Clause:
    start: int
    end: int
    question: str | None  # "yesno", "wh" or None
    quote_at: int | None  # absolute position of the reported-speech marker
    endorsed: bool


def clauses(raw: str) -> list[Clause]:
    """Split lower-cased, whitespace-collapsed text into sentence clauses.
    Positions are shared with ``_normalize`` of the same string."""
    result = []
    for sentence in re.finditer(r"[^.!?…]+[.!?…]*", raw):
        body, ending = sentence.group(0), sentence.group(0).rstrip()
        asked = ending.endswith("?")
        cuts = [0] + [match.end() for match in _CONNECTIVE.finditer(body)] + [len(body)]
        spans = [(sentence.start() + cuts[index], sentence.start() + cuts[index + 1]) for index in range(len(cuts) - 1)]
        spans = [span for span in spans if raw[span[0]:span[1]].strip(" ,;.!?…")]
        for number, (start, end) in enumerate(spans):
            text = raw[start:end]
            norm = _normalize(text)
            question = "wh" if _WH.search(norm) else "yesno" if (_YESNO.search(norm) or (asked and number == len(spans) - 1)) else None
            quote = _QUOTE.search(text)
            result.append(Clause(start, end, question, start + quote.start() if quote else None, bool(_ENDORSE.search(text))))
    return result


class View:
    """Raw and normalized text with clause lookup by position."""

    def __init__(self, text: str):
        self.raw = " ".join(text.lower().split())
        normalized = _normalize(text)
        # _normalize keeps one character per character of the collapsed text.
        self.norm = normalized if len(normalized) == len(self.raw) else _normalize(self.raw)
        self.aligned = len(self.norm) == len(self.raw)
        self.clauses = clauses(self.raw) if self.aligned else []
        self.reported_quotes = []
        for quote in _REPORTED_QUOTE.finditer(self.raw):
            tail = re.split(r"[.!?…]", self.raw[quote.end():], maxsplit=1)[0]
            if _POST_QUOTE_SPEAKER.match(tail) and not _ENDORSE.search(tail):
                self.reported_quotes.append(quote.span())

    def clause_at(self, position: int) -> Clause | None:
        return next((clause for clause in self.clauses if clause.start <= position < clause.end), None)

    def attributed(self, clause: Clause | None, position: int) -> bool:
        """The latest reported-speech marker before ``position`` still governs it."""
        if any(start <= position < end for start, end in self.reported_quotes):
            return True
        if not clause or clause.quote_at is None or clause.quote_at > position or clause.endorsed:
            return False
        markers = list(_QUOTE.finditer(self.raw[clause.start:position]))
        if not markers:
            return False
        return not _QUOTE_BREAK.search(self.norm[clause.start + markers[-1].end():position])

    def prefix(self, clause: Clause | None, position: int) -> str:
        return self.norm[clause.start:position] if clause else self.norm[:position]

    def suffix(self, clause: Clause | None, position: int) -> str:
        return self.norm[position:clause.end] if clause else self.norm[position:]

    def scope(self, clause: Clause | None, position: int) -> str:
        """Clause text before ``position`` after the last denial break."""
        before = self.prefix(clause, position)
        cuts = [match.end() for match in _DENIAL_BREAK.finditer(before)]
        return before[cuts[-1]:] if cuts else before

    def broken_before(self, clause: Clause | None, position: int) -> bool:
        return bool(_DENIAL_BREAK.search(self.prefix(clause, position)))

    def questioned(self, clause: Clause | None, start: int, end: int) -> bool:
        """A yes/no question about the matched content, not one wrapping an assertion."""
        if not clause or not clause.question:
            return False
        segment = self.norm[clause.start:clause.end].rstrip()
        after = self.norm[end:clause.end]
        if _COMPLETION_QUESTION.search(segment) or _TAG_QUESTION.search(segment):
            return True
        if re.search(r"\b(?:duoc khong|hay khong)\b", after):
            return True
        # "có ... không?" around the match. A "có" after the match asks something else.
        return bool(re.search(r"\bco\b", self.scope(clause, start)) and re.search(r"\bkhong\b\s*[?.!…]*\s*$", after))

    def denied(self, clause: Clause | None, start: int, end: int, *, claim_free_prefix: bool = False) -> bool:
        """Exactly one negator governs the match in its scope, or a refusal follows it."""
        if clause:
            segment = self.norm[clause.start:clause.end]
            expectation = _NEGATIVE_EXPECTATION.match(segment)
            if (expectation and start >= clause.start + expectation.start()
                    and not _DENIAL_BREAK.search(self.norm[clause.start + expectation.end():start])):
                return True
            # "Em có quyền hứa bồi thường hay giảm giá đâu?" disputes
            # authority. The suffix must contain only the remedy list and đâu.
            before = self.prefix(clause, start)
            after = _REMEDY.sub("", self.suffix(clause, end))
            if (re.search(r"(?:^|\bchu\s+)(?:(?:em|ben em|cua hang|shop|tiem)\s+)?"
                          r"co\s+(?:quyen\s+)?(?:hua\s+|cho\s+)?$", before)
                    and re.fullmatch(r"\s*(?:(?:hay|va|hoac|hay la|cung nhu|hoac la)\s+)*dau\s*[?.!…]*", after)):
                return True
        before = self.scope(clause, start)
        negators = list(_NEGATOR.finditer(before))
        if len(negators) == 1:
            tail = before[negators[0].end():]
            if _OTHER_PREDICATE.search(tail):
                return False
            near = len(tail.split()) <= 3
            verb = re.match(r"\s+(?:\S+\s+){0,2}?(?:can|phai|bat buoc|yeu cau|doi hoi|quy dinh|noi|ghi|co dieu kien|"
                            r"nghi|tin|thay|chap nhan|dong y|muon|lay|nhan|dam|duoc phep)\b", tail)
            return bool(near or verb)
        if negators:
            return False
        if claim_free_prefix and _CLAIM.search(self.prefix(clause, start)):
            return False
        return bool(_TRAILING_DENIAL.search(self.suffix(clause, end)))

    def spans(self, clause_a: Clause | None, clause_b: Clause | None) -> list[Clause]:
        if clause_a is None or clause_b is None:
            return [clause for clause in (clause_a, clause_b) if clause]
        return [clause for clause in self.clauses if clause_a.start <= clause.start <= clause_b.start]


# A refusal right after the match: "hoàn tiền thì chị không cần", "... là sai".
# Only small fillers may intervene, and "không sai" is not a refusal.
_TRAILING_DENIAL = re.compile(r"^(?:\s+(?:cho chi|cho em|lai|nua|ay|do|nay|gi))*\s*(?:(?:thi|la|the|vay)\s+)*"
                              r"(?:(?:chi|em|cua hang)\s+)?(?:(?:khong|chang|chua)\s+(?:can|muon|quan tam|lay|nhan|"
                              r"chap nhan|dong y|hop ly|co trong)\b|(?:vo ly|vo li|sai|khong dung)\b)")
_CLAIM = re.compile(r"\b(?:da|vua|roi|xong|se|dong y|chap nhan|duyet|cho phep|duoc phep|quyet dinh|hua|cam ket)\b|\d|phan tram")
_DEMAND = re.compile(r"\bchi\s+(?:(?:chi|van|rat|cung)\s+)?(?:muon|can|xin|de nghi|yeu cau|mong|doi hoi)\b"
                     r"|(?:\s(?:di|nhe|nha|giup chi|cho chi|duoc khong))\s*[.!?]*$")
# Lan asking for something, in any tense: "chị đã xin hoàn tiền".
_OWN_REQUEST = re.compile(r"(?:^\s*|\bchi\s+(?:(?:da|van|chi|rat|cung|dang|vua)\s+)*)(?:xin|muon|can|de nghi|yeu cau|"
                          r"doi hoi|mong)\b(?:\s+\S+){0,2}\s*$"
                          r"|\bchi\s+(?:khong|chang)\s+muon\s+(?:khong|chang)\s+(?:duoc\s+)?$")
# A store approval, or Lan saying she received the remedy.
_STORE_CLAIM = re.compile(r"(?:\bem|cua hang|tiem|shop|ben em|quan ly|nhan vien)\s+(?:(?:da|vua|se|cung|van|dang)\s+)*"
                          r"(?:dong y|chap nhan|duyet|cho phep|quyet dinh|hua|cam ket)\b"
                          r"|(?:\bem|cua hang|tiem|shop|ben em|quan ly)\s+(?:da|vua|se)\s"
                          r"|\bchi\s+(?:(?:da|vua|se|cung)\s+)*(?:duoc|nhan duoc|nhan|lay lai)\b")

# ------------------------------------------------------------------ policy patterns

_EXTRA = (r"(?:hoa don|bien lai|\b(?:nguyen |con )?tem\b|(?:con|co|kem|giu|mang theo) hop\b|hop giay|"
          r"chua (?:qua )?(?:su dung|dung|mang(?![^.!?]{0,25}\btheo\b)|di)|khong (?:duoc )?su dung)")
_CONDITIONS = (
    re.compile(r"(?:neu|dieu kien|yeu cau|bat buoc|phai|can co|chi duoc doi|chi doi (?:duoc )?(?:khi|neu))[^.!?]{0,60}" + _EXTRA),
    re.compile(_EXTRA + r"[^.!?]{0,40}(?:moi|thi|de) (?:duoc |co the )?doi"),
    re.compile(r"(?:khong|thieu|mat|neu khong)\s+(?:co\s+)?" + _EXTRA + r"[^.!?]{0,30}(?:khong|chang)\s+(?:the\s+)?(?:duoc\s+)?doi"),
)
_REMEDY = re.compile(r"(?:hoan (?:lai )?tien|tra (?:lai )?tien|lay lai tien|giam gia|chiet khau|boi thuong|den bu|"
                     r"phieu (?:giam|mua hang|qua)|voucher|ma giam|tang (?:qua|kem|them)|mien phi|bao hanh)")
_GUARANTEE = re.compile(r"(?:chac chan|dam bao|bao dam|cam ket|100 ?%|mot tram phan tram|tuyet doi)[^.!?]{0,30}"
                        r"(?:khong (?:bi )?dau|het dau|vua|hop|em chan|thoai mai)|khong bao gio (?:bi )?dau")
# v1's completed or third-party store actions.
_STORE_ACTION = re.compile(
    r"(?:\bem|cua hang|tiem|shop|ben em|quan ly)\s+(?:da|vua|se)\s+(?:dong y\s+|chap nhan\s+|duyet\s+)?(?:cho chi\s+)?"
    r"(?:doi|hoan|tang|giam|boi thuong|xu ly xong|giai quyet xong)"
    r"|(?:\bem|cua hang|tiem|shop|ben em|quan ly|nhan vien)\s+(?:(?:da|vua|se)\s+)?"
    r"(?:dong y|chap nhan|duyet|cho phep|quyet dinh)\b"
    r"|quan ly\s+(?:da|se|dong y|chap nhan)\b"
    r"|(?:da|vua)\s+doi\s+(?:giay\s+)?cho chi")
# Legacy completion and physical-action patterns, applied with clause status.
_COMPLETION = re.compile(r"(?:da (?:kiem tra|kham|mang thu|di thu|thu doi)|thu xong|(?:kiem tra|kham|mang thu|di thu|thu giay|"
                         r"thu doi)[^.!?]{0,20}(?:roi|xong|cho thay)|(?:thu|mang) doi moi[^.!?]{0,20}(?:thay|rat|hoan toan) "
                         r"(?:em|vua|thoai mai)|ket qua (?:kiem tra|kham))")
_LATER = r"(?![^.!?]{0,30}(?:sau|hom khac|hom sau|lan sau|buoi khac|dip khac|quay lai|ghe lai|mang[^.!?]{0,15}lai))"
_PHYSICAL = re.compile(r"chi (?:se |xin )?(?:dua|giao|gui)[^.!?]{0,12}(?:giay|doi)" + _LATER
                       + r"|chi (?:dang|da|se|dong y) (?:mang |di )?thu (?:doi|giay|ngay|luon)" + _LATER
                       + r"|(?:chi|de chi) (?:mang |di )?thu (?:ngay|luon|lien|o day|bay gio|tai cho)"
                       + r"|chi (?:coi|thao|xo chan vao)|day em (?:xem|kiem tra|cam)")
_SATISFIED = re.compile(r"(?<!khong )(?<!chua )(?<!chang )chi (?:rat |thay |da )?(?:hai long|yen tam|an tam)")
_DISSATISFIED = re.compile(r"(?:chua|khong|chang) (?:thuc su )?(?:hai long|yen tam|an tam)|(?:chua|khong) giai quyet|"
                           r"khong chap nhan|that vong|mat niem tin|quan ly|phan anh")
_RESERVATION = re.compile(r"chua (?:thuc su )?(?:hai long|yen tam)|van (?:lo|con lo|chua)|can nhac|suy nghi them|de chi xem")
_CLOSURE = ("chi ve", "chao em", "ket thuc", "dung o day", "dung trao doi", "khong noi chuyen", "ve truoc", "di ve",
            "thoi chi", "chi xin phep", "de hom khac", "luc khac", "chi di day", "chi di nhe")
# Receipt/box/tag/condition vocabulary is also how store policy is discussed.
# For these facts a bare mention is a disclosure only in Lan's own non-policy
# statement; their assertion/contradiction patterns still count anywhere.
_POLICY_VOCAB_FACTS = frozenset({"receipt_status", "packaging_status", "shoe_condition", "product_details"})
_POLICY_CONTEXT = re.compile(r"\b(?:chinh sach|quy dinh|dieu kien|yeu cau|bat buoc|neu|phai|can co|moi (?:duoc )?doi|duoc doi|doi tra)\b")
_REQUIREMENT_MENTION = re.compile(r"(?:phai|can|bat buoc|yeu cau|doi hoi|xuat trinh|trinh)\s+(?:co\s+|giu\s+|mang\s+)?"
                                  r"(?:hoa don|bien lai|hop giay|hop|tem(?: giay)?)")


def _mask(text: str, spans) -> str:
    chars = list(text)
    for start, end in spans:
        chars[start:end] = " " * (end - start)
    return "".join(chars)


def _source_spans(norm: str, texts: Mapping) -> list[tuple[int, int]]:
    spans = []
    for source in sorted(texts.values(), key=len, reverse=True):
        needle = _normalize(source).rstrip(".!?")
        if not needle:
            continue
        for match in re.finditer(re.escape(needle), norm):
            spans.append(match.span())
    return spans


def _exempt(view: View, match, *, questions: str, allow_denial: bool = True, claim_free_prefix: bool = False) -> bool:
    """True when a match is not Lan's own assertion.

    ``questions``: "any" also exempts wh-questions that do not presuppose the
    match after a break, "yesno" only yes/no questions about the match, "none"
    no question. Clauses from start to end must all be exempt."""
    first, last = view.clause_at(match.start()), view.clause_at(max(match.start(), match.end() - 1))
    covered = view.spans(first, last) or [None]
    for clause in covered:
        position = match.start() if clause is first else (clause.start if clause else match.start())
        if view.attributed(clause, position):
            continue
        if questions != "none" and view.questioned(clause, position, match.end()):
            continue
        if (questions == "any" and clause and clause.question == "wh"
                and not view.broken_before(clause, position)):
            continue
        if allow_denial and clause is first and view.denied(clause, match.start(), match.end(),
                                                            claim_free_prefix=claim_free_prefix):
            continue
        return False
    return True


# ------------------------------------------------------------------ checks

def policy_findings(text: str) -> tuple[list[str], list[str]]:
    """Assertion-aware store-policy and authority checks. Returns (blocking, warnings)."""
    view = View(text)
    norm, blocking, warnings = view.norm, [], []
    if any(phrase in norm for phrase in _INSTRUCTION_LEAK) or _KEY_LEAK.search(text):
        blocking.append("instruction_leak")
    for pattern in _CONDITIONS:
        if any(not _exempt(view, match, questions="any") for match in pattern.finditer(norm)):
            blocking.append("invented_policy_condition")
            break
    previous_denied_end = None
    for match in _REMEDY.finditer(norm):
        clause = view.clause_at(match.start())
        between = norm[previous_denied_end:match.start()] if previous_denied_end is not None else None
        if between is not None and clause and re.fullmatch(r"\s*(?:,|hay|va|hoac|hay la|cung nhu|hoac la|\s)*\s*", between):
            previous_denied_end = match.end()  # "không cần hoàn tiền hay giảm giá"
            continue
        clause_text = norm[clause.start:clause.end] if clause else norm
        claim = bool(_CLAIM.search(clause_text))
        if view.attributed(clause, match.start()):
            continue
        if view.questioned(clause, match.start(), match.end()):
            continue
        if clause and clause.question == "wh" and not claim and not view.broken_before(clause, match.start()):
            continue
        if view.denied(clause, match.start(), match.end(), claim_free_prefix=True):
            previous_denied_end = match.end()
            continue
        previous_denied_end = None
        own = _OWN_REQUEST.search(view.scope(clause, match.start()))
        if not _STORE_CLAIM.search(clause_text) and (own or (not claim and _DEMAND.search(clause_text))):
            warnings.append("customer_requests_unauthorized_remedy")
            continue
        blocking.append("unauthorized_remedy_claim")
        break
    for match in _GUARANTEE.finditer(norm):
        clause = view.clause_at(match.start())
        if _exempt(view, match, questions="any"):
            continue
        if _DEMAND.search(norm[clause.start:clause.end] if clause else norm) and not re.search(r"\b(?:chi|doi nay|doi moi|giay)\b[^.!?]{0,8}$",
                                                                                                 view.prefix(clause, match.start())):
            warnings.append("customer_requests_unauthorized_remedy")
            continue
        blocking.append("unauthorized_guarantee_claim")
        break
    if any(not _exempt(view, match, questions="yesno") for match in _STORE_ACTION.finditer(norm)):
        blocking.append("fabricated_store_action")
    for kind, period, match in _period_matches(view):
        if _exempt(view, match, questions="any"):
            continue
        if kind == "policy" or not _valid_period(kind, period):
            blocking.append("wrong_purchase_period" if kind == "purchase" else "invented_policy")
    return list(dict.fromkeys(blocking)), list(dict.fromkeys(warnings))


class _Span:
    def __init__(self, start, end):
        self._start, self._end = start, end

    def start(self):
        return self._start

    def end(self):
        return self._end


def _period_matches(view: View):
    """Legacy period claims, located per clause so quoted or denied ones are exempt."""
    found = []
    for clause in view.clauses:
        segment = view.norm[clause.start:clause.end]
        for kind, period in _period_claims(segment):
            position = segment.find(period)
            start = clause.start + max(position, 0)
            found.append((kind, period, _Span(start, start + max(len(period), 1))))
        raw = view.raw[clause.start:clause.end]
        for match in re.finditer(r"(?:đổi|trả|chính sách).{0,40}(?:tuần|tháng|năm)(?!\w)", raw):
            found.append(("policy", "", _Span(clause.start + match.start(), clause.start + match.end())))
    return found


def completion_findings(text: str, view: View | None = None, masked: str | None = None) -> list[str]:
    view = view or View(text)
    norm = masked if masked is not None else view.norm
    codes = []
    if any(not _exempt(view, match, questions="yesno") for match in _COMPLETION.finditer(norm)):
        codes.append("unsupported_completion")
    if any(not _exempt(view, match, questions="none", allow_denial=False) for match in _PHYSICAL.finditer(norm)):
        codes.append("physical_action")
    return codes


def fact_findings(text: str, plan: Mapping, *, trust_challenge: bool = False) -> list[str]:
    """Disclosure and contradiction checks over Lan's own statements."""
    view = View(text)
    known = _fact_ids(plan.get("knownFacts", []))
    allowed = set(_fact_ids(plan.get("allowedNewFacts", []))) | set(known)
    if trust_challenge:
        allowed.add("prior_advice")
    texts = fact_texts(list(allowed), plan.get("factTexts"))
    ungrounded = _mask(view.norm, _source_spans(view.norm, texts))
    facts_view = _mask(ungrounded, [match.span() for match in _REQUIREMENT_MENTION.finditer(ungrounded)])
    codes = []
    for fact, markers in _FACT_MARKERS.items():
        assertions, contradictions = _FACT_ASSERTIONS.get(fact, ()), _FACT_CONTRADICTIONS.get(fact, ())
        patterns = [match for pattern in (*assertions, *contradictions) for match in re.finditer(pattern, facts_view)]
        marker_hits = [match for marker in markers for match in re.finditer(re.escape(marker), facts_view)]
        if fact in _POLICY_VOCAB_FACTS:
            own = []
            for match in marker_hits:
                clause = view.clause_at(match.start())
                segment = view.norm[clause.start:clause.end] if clause else view.norm
                if not (clause and clause.question) and not view.attributed(clause, match.start()) and not _POLICY_CONTEXT.search(segment):
                    own.append(match)
            marker_hits = own
        if (plan.get("naturalFactParaphrases") and fact == "shoe_condition" and "fit_condition" in allowed
                and "nguyen ven" in _normalize(texts.get("fit_condition", ""))):
            # Intactness is already part of the fit fact. Wear history remains
            # separate and still needs authorization through its assertions.
            marker_hits = [hit for hit in marker_hits if hit.group() not in
                           ("con nguyen ven", "khong bi hong", "khong hong")]
        if fact not in allowed:
            if patterns or marker_hits:
                codes.append("unallowed_fact")
            continue
        contradicted = any(not _exempt(view, match, questions="any", allow_denial=False)
                           for pattern in contradictions for match in re.finditer(pattern, facts_view))
        overridden = fact in texts and texts[fact] != FACTS[fact] and bool(patterns or marker_hits)
        if contradicted or overridden:
            codes.append("contradicted_fact")
    codes += completion_findings(text, view, ungrounded)
    return list(dict.fromkeys(codes))


def ending_tone(text: str, tone: str | None) -> str:
    """'ok', 'conflict' (blocking) or 'unverified' (warning) for a code-chosen ending tone."""
    if not tone:
        return "ok"
    norm = _normalize(text)
    satisfied, dissatisfied = bool(_SATISFIED.search(norm)), bool(_DISSATISFIED.search(norm))
    if tone == "ending_restored":
        return "conflict" if dissatisfied else "ok" if satisfied else "unverified"
    if tone == "ending_partially_restored":
        reserved = bool(_RESERVATION.search(norm))
        return "ok" if reserved else "conflict" if satisfied else "unverified"
    if tone == "ending_lost":
        return "conflict" if satisfied else "ok" if dissatisfied else "unverified"
    if tone == "ending_exchange_accepted":
        if satisfied or re.search(r"chi (?:khong|chang) (?:dong y|chap nhan|doi)", norm):
            return "conflict"
        return "ok" if re.search(r"chi (?:dong y|chap nhan|doi)|doi (?:theo|cho chi)|(?:xu ly|lam) nhu vay", norm) else "unverified"
    if tone == "ending_review":
        return "conflict" if satisfied or "mat niem tin" in norm else "ok"
    return "unverified"


def sentences(text: str) -> list[str]:
    return [part for part in re.split(r"(?<!\d)[.!?…]+|[.!?…]+(?!\d)", text) if part.strip()]


def evaluate(text: Any, guard_plan: Mapping, transcript: str, *, ending_tone_key: str | None, soft_close: bool,
             trust_challenge: bool, allow_hearing_clarification: bool = False) -> tuple[list[str], list[str]]:
    """Hard codes block the candidate; warnings are diagnostics only."""
    if not isinstance(text, str) or not text.strip():
        return ["missing_text"], []
    text = text.strip()
    legacy_plan = dict(guard_plan)
    legacy_plan["requiredContent"] = [key for key in guard_plan.get("requiredContent") or []
                                      if not str(key).startswith("ending_")]
    legacy = customer_text_rejections(text, legacy_plan, transcript)
    blocking = [code for code in legacy if code in LEGACY_HARD]
    if guard_plan.get("naturalFactParaphrases") and "missing_required_fact" in blocking:
        normalized = re.sub(r"\bdung (?:size|kich co)\b", "dung co", _normalize(text))
        required = _fact_ids(guard_plan.get("requiredFactIds", []))
        if guard_plan.get("intent") == "confirm_fact":
            required = list(dict.fromkeys(required + _fact_ids(guard_plan.get("confirmedFactIds", []))))
        allowed = set(_fact_ids(guard_plan.get("knownFacts", [])) + _fact_ids(guard_plan.get("allowedNewFacts", [])))
        texts = fact_texts(list(allowed), guard_plan.get("factTexts"))
        if all(fact in allowed and (_normalize(texts.get(fact, "")).rstrip(".!?") in normalized
            or (texts.get(fact) == FACTS[fact] and _asserted_fact(normalized, fact))) for fact in required):
            blocking.remove("missing_required_fact")
    if allow_hearing_clarification:
        blocking = [code for code in blocking if code != "false_hearing_failure"]
    warnings = [code for code in legacy if code in LEGACY_WARNINGS]
    policy_block, policy_warn = policy_findings(text)
    blocking += policy_block + fact_findings(text, guard_plan, trust_challenge=trust_challenge)
    warnings += policy_warn
    raw = " ".join(text.lower().split())
    if _WRONG_ADDRESS.search(raw):
        blocking.append("wrong_address")
    elif not re.search(r"(?<!\w)chị(?!\w)", raw):
        (warnings if guard_plan.get("naturalFactParaphrases") else blocking).append(
            "implicit_address" if guard_plan.get("naturalFactParaphrases") else "wrong_address")
    words = len(text.split())
    if len(text) > HARD_MAX_CHARS or words > HARD_MAX_WORDS:
        blocking.append("too_long")
    elif words > SOFT_MAX_WORDS:
        warnings.append("long_reply")
    if len(sentences(text)) > SOFT_MAX_SENTENCES:
        warnings.append("too_many_sentences")
    tone = ending_tone(text, ending_tone_key)
    if tone == "conflict":
        blocking.append("ending_tone_conflict")
    elif tone == "unverified":
        warnings.append("ending_tone_unverified")
    norm = _normalize(text)
    if soft_close:
        view = View(text)
        if any(not _exempt(view, match, questions="any", allow_denial=False) for match in _SATISFIED.finditer(norm)):
            blocking.append("unsupported_satisfaction")
        if not any(anchor in norm for anchor in _CLOSURE):
            warnings.append("closure_not_explicit")
    return list(dict.fromkeys(blocking)), list(dict.fromkeys(warnings))


__all__ = ["evaluate", "policy_findings", "fact_findings", "completion_findings", "ending_tone", "clauses", "View",
           "SCENARIO", "_required_content_present"]
