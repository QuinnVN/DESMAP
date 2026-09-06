# **D: DESIRE & CAREER ORIENTATION**

### **Mục đích**

D đo **những điều người chơi mong muốn và coi trọng khi lựa chọn nghề nghiệp**.

D trả lời câu hỏi (giá trị nghề nghiệp) 

> **“Nếu chọn một nghề, tôi muốn nghề đó mang lại điều gì cho mình?”**

D **không đo năng lực** và **không đo ngành người chơi yêu thích**. Phần ngành yêu thích sẽ được xác định ở bước **Career Interest** trước DESMAP.

Theo lý thuyết 16 nhu cầu cơ bản của Steven Reiss hoặc các khía cạnh khao khát nghề nghiệp, động lực mong muốn cốt lõi của con người trong công việc thường bao gồm các nhóm chính 

## **1\. Cấu trúc D**

D gồm **6 sub-dimensions**, mỗi nhóm **3 câu**, tổng cộng **18 câu**.

| Mã | Dimension | Nội dung |
| ----- | ----- | ----- |
| **D1** | Financial & Security | Thu nhập, phúc lợi, sự ổn định |
| **D2** | Growth & Achievement | Học hỏi, phát triển, thành tựu, thử thách |
| **D3** | Autonomy | Tự chủ, tự quyết định cách làm việc |
| **D4** | Meaning & Contribution | Ý nghĩa, đóng góp, tạo giá trị |
| **D5** | Recognition & Status | Công nhận, địa vị, tiếng nói, ảnh hưởng |
| **D6** | Lifestyle & Balance | Cân bằng công việc – cuộc sống |

# **2\. Logic chấm điểm**

Mỗi câu có **3 lựa chọn**.

* Lựa chọn thể hiện **mong muốn cao** → `+2`  
* Lựa chọn trung gian → `+1`  
* Lựa chọn thể hiện **mong muốn thấp** → `+0`

Mỗi dimension có 3 câu:

> **Điểm tối đa mỗi D \= 6**

Quy đổi:

| Điểm | % |
| ----- | ----- |
| 0/6 | 0% |
| 1/6 | 17% |
| 2/6 | 33% |
| 3/6 | 50% |
| 4/6 | 67% |
| 5/6 | 83% |
| 6/6 | 100% |

**Lưu ý cho IT:** không nên hard-code rằng A luôn \= 2, B \= 1, C \= 0\. Có thể đảo vị trí đáp án để tránh người chơi đoán pattern. Database chỉ cần lưu:

`option → dimension → score`

# **3\. Bộ 18 câu**

## **D1 \- Financial & Security**

### **D1.1**

Nếu hai công việc tương đương về nội dung và cơ hội phát triển, bạn chọn:

**A.** Thu nhập cao hơn nhưng có nhiều biến động. → `D1 +2`  
**B.** Thu nhập và mức ổn định ở mức cân bằng. → `D1 +1`  
**C.** Thu nhập thấp hơn nhưng công việc rất ổn định. → `D1 +0`

### **D1.2**

Điều nào khiến bạn yên tâm nhất khi nhận một công việc?

**A.** Thu nhập cao và có khả năng tăng nhanh. → `D1 +2`  
**B.** Lương ổn định và có phúc lợi cơ bản. → `D1 +1`  
**C.** Môi trường thú vị dù thu nhập chưa cao. → `D1 +0`

### **D1.3**

Bạn được chọn giữa:

**A.** Công việc có thu nhập rất cao nhưng khả năng mất việc cao. → `D1 +2`  
**B.** Công việc có thu nhập khá và tương đối ổn định. → `D1 +1`  
**C.** Công việc thu nhập thấp hơn nhưng có rất ít rủi ro. → `D1 +0`

## **D2 \- Growth & Achievement**

### **D2.1**

Bạn thích công việc nào hơn?

**A.** Luôn có mục tiêu mới để chinh phục. → `D2 +2`  
**B.** Có cơ hội phát triển nhưng với tốc độ vừa phải. → `D2 +1`  
**C.** Công việc ổn định, không yêu cầu phải liên tục tiến bộ. → `D2 +0`

### **D2.2**

Nếu được giao một nhiệm vụ vượt quá khả năng hiện tại:

**A.** Tôi thấy đây là cơ hội để thử giới hạn. → `D2 +2`  
**B.** Tôi sẽ cân nhắc rồi quyết định. → `D2 +1`  
**C.** Tôi thích nhiệm vụ nằm trong vùng năng lực hiện tại hơn. → `D2 +0`

### **D2.3**

Điều nào hấp dẫn bạn nhất?

**A.** Được thăng tiến nhanh nhờ thành tích. → `D2 +2`  
**B.** Có cơ hội học hỏi và phát triển ổn định. → `D2 +1`  
**C.** Có công việc ổn định mà không phải cạnh tranh quá nhiều. → `D2 +0`

## **D3 \- Autonomy**

### **D3.1**

Bạn thích kiểu quản lý nào?

**A.** Giao mục tiêu, còn cách thực hiện để tôi tự quyết định. → `D3 +2`  
**B.** Có mục tiêu rõ và một số hướng dẫn. → `D3 +1`  
**C.** Được hướng dẫn cụ thể từng bước. → `D3 +0`

### **D3.2**

Khi làm một dự án, điều nào quan trọng nhất với bạn?

**A.** Tôi được tự quyết định cách làm. → `D3 +2`  
**B.** Tôi được tự quyết một phần nhưng vẫn có định hướng. → `D3 +1`  
**C.** Tôi muốn có quy trình rõ ràng để làm theo. → `D3 +0`

### **D3.3**

Nếu công ty cho phép bạn chọn thời gian làm việc miễn hoàn thành mục tiêu:

**A.** Tôi rất thích cách làm này. → `D3 +2`  
**B.** Tôi thấy khá phù hợp. → `D3 +1`  
**C.** Tôi thích lịch làm việc cố định hơn. → `D3 +0`

## **D4 \- Meaning & Contribution**

### **D4.1**

Nếu hai công việc có mức lương tương đương, bạn nghiêng về:

**A.** Công việc trực tiếp tạo ra giá trị cho cộng đồng. → `D4 +2`  
**B.** Công việc vừa có ích vừa có lợi ích cá nhân. → `D4 +1`  
**C.** Công việc chủ yếu phục vụ mục tiêu cá nhân. → `D4 +0`

### **D4.2**

Điều gì khiến bạn cảm thấy công việc “đáng làm” nhất?

**A.** Biết rằng công việc của mình giúp giải quyết một vấn đề có ý nghĩa. → `D4 +2`  
**B.** Công việc vừa có ý nghĩa vừa giúp tôi phát triển. → `D4 +1`  
**C.** Công việc mang lại thành công cho chính tôi. → `D4 +0`

### **D4.3**

Bạn sẽ cân nhắc thế nào về một công việc có thu nhập thấp hơn nhưng tạo tác động xã hội rõ rệt?

**A.** Tôi sẵn sàng cân nhắc nghiêm túc. → `D4 +2`  
**B.** Tôi sẽ cân nhắc nếu các điều kiện khác phù hợp. → `D4 +1`  
**C.** Tôi ưu tiên công việc mang lại lợi ích cá nhân cao hơn. → `D4 +0`

## **D5 \- Recognition & Status**

### **D5.1**

Điều nào khiến bạn cảm thấy thành công nhất?

**A.** Được công nhận là người có năng lực và có ảnh hưởng. → `D5 +2`  
**B.** Được ghi nhận khi hoàn thành tốt công việc. → `D5 +1`  
**C.** Biết bản thân làm tốt là đủ. → `D5 +0`

### **D5.2**

Trong một tổ chức, vị trí nào hấp dẫn bạn hơn?

**A.** Vị trí có quyền quyết định và tiếng nói lớn. → `D5 +2`  
**B.** Vị trí có uy tín và được đồng nghiệp tôn trọng. → `D5 +1`  
**C.** Vị trí chuyên môn tốt nhưng không cần nhiều sự chú ý. → `D5 +0`

### **D5.3**

Nếu thành tích của bạn không được công khai, bạn sẽ:

**A.** Khá thất vọng vì tôi muốn thành quả được ghi nhận. → `D5 +2`  
**B.** Hơi tiếc nhưng không quá quan trọng. → `D5 +1`  
**C.** Không vấn đề gì, kết quả quan trọng hơn sự công nhận. → `D5 +0`

## **D6 \- Lifestyle & Balance**

### **D6.1**

Bạn ưu tiên công việc nào?

**A.** Có thời gian ổn định cho gia đình, bạn bè và bản thân. → `D6 +2`  
**B.** Có thể bận vào một số giai đoạn nhưng vẫn có thời gian nghỉ. → `D6 +1`  
**C.** Sẵn sàng dành phần lớn thời gian cho sự nghiệp nếu cơ hội đủ tốt. → `D6 +0`

### **D6.2**

Nếu một công việc yêu cầu thường xuyên làm ngoài giờ để đổi lấy cơ hội thăng tiến nhanh:

**A.** Tôi không muốn đánh đổi quá nhiều thời gian cá nhân. → `D6 +2`  
**B.** Tôi chấp nhận trong một khoảng thời gian nhất định. → `D6 +1`  
**C.** Tôi sẵn sàng hy sinh thời gian cá nhân để phát triển sự nghiệp. → `D6 +0`

### **D6.3**

Môi trường nào phù hợp với bạn nhất?

**A.** Thời gian linh hoạt, miễn hoàn thành công việc. → `D6 +2`  
**B.** Lịch làm việc tương đối ổn định nhưng có một số linh hoạt. → `D6 +1`  
**C.** Lịch làm việc cố định và ưu tiên tối đa cho công việc. → `D6 +0`

# **4\. Output cho người chơi**

Ví dụ:

> **D1 \- Financial & Security: 50%**  
> **D2 \- Growth & Achievement: 83%**  
> **D3 \- Autonomy: 100%**  
> **D4 \- Meaning & Contribution: 67%**  
> **D5 \- Recognition & Status: 50%**  
> **D6 \- Lifestyle & Balance: 83%**

AI sau đó đọc **pattern 6 chỉ số**, ví dụ:

> **Dominant desires:** Autonomy \+ Growth \+ Lifestyle  
> Người chơi có xu hướng tìm kiếm một nghề cho phép phát triển, tự chủ trong cách làm việc nhưng vẫn duy trì chất lượng cuộc sống.

# **E \- EXPERTISE**

Dựa vào Các nền tảng lý thuyết gốc

* **Mô hình Khung năng lực 3 thành phần (KSAOs \- Knowledge, Skills, Abilities, and Other characteristics):** Được nghiên cứu rộng rãi từ những năm 1950–1970 trong tâm lý học công nghiệp/tổ chức (I/O Psychology).  
* **Lý thuyết Kỹ năng quản lý của Robert Katz (1955):** Phân chia năng lực cốt lõi thành kỹ năng kỹ thuật (Technical), kỹ năng nhận thức/tư duy (Conceptual/Cognitive) và kỹ năng con người (Human/Interpersonal).  
* **Khung năng lực toàn diện (Competency-based Management):** Được chuẩn hóa bởi các tổ chức nhân sự lớn trên thế giới như SHRM (Society for Human Resource Management) hoặc các nghiên cứu về hiệu suất làm việc nhóm (Teamwork and Performance Expertise).

**E đo dạng năng lực mà người chơi có xu hướng phát huy trong công việc.**

Gồm 3 nhóm:

* **E1 — Technical Expertise:** chuyên môn kỹ thuật  
* **E2 — Cognitive Expertise:** tư duy & giải quyết vấn đề  
* **E3 — Interpersonal Expertise:** giao tiếp & kết nối

**Mỗi nhóm 5 câu. Mỗi câu 3 lựa chọn.**

# **E1 \- TECHNICAL EXPERTISE**

### **Chuyên môn kỹ thuật**

### **E1.1**

Khi học một lĩnh vực mới, điều nào khiến bạn hứng thú nhất?

**A.** Tìm hiểu cách một công cụ hoặc hệ thống cụ thể hoạt động. → `E1 +2`  
 **B.** Hiểu nguyên lý rồi thử áp dụng vào thực tế. → `E1 +1`  
 **C.** Tìm hiểu lĩnh vực đó ảnh hưởng đến con người hoặc xã hội như thế nào. → `E1 +0`

### **E1.2**

Bạn thích loại nhiệm vụ nào hơn?

**A.** Sử dụng kiến thức chuyên môn để tạo ra một sản phẩm hoặc kết quả cụ thể. → `E1 +2`  
 **B.** Kết hợp chuyên môn với những loại công việc khác. → `E1 +1`  
 **C.** Làm việc chủ yếu với ý tưởng hoặc con người hơn là công cụ chuyên môn. → `E1 +0`

### **E1.3**

Khi đã sử dụng một công cụ nhiều lần, bạn thường:

**A.** Muốn hiểu sâu và thành thạo nó. → `E1 +2`  
 **B.** Biết đủ để sử dụng hiệu quả. → `E1 +1`  
 **C.** Chỉ muốn biết cách sử dụng cơ bản rồi chuyển sang thứ khác. → `E1 +0`

### **E1.4**

Bạn cảm thấy tự tin nhất khi:

**A.** Có thể tự mình thực hiện một nhiệm vụ đòi hỏi chuyên môn cụ thể. → `E1 +2`  
 **B.** Có thể thực hiện nhiệm vụ sau khi được hướng dẫn. → `E1 +1`  
 **C.** Có thể đóng góp ở những phần không yêu cầu chuyên môn kỹ thuật sâu. → `E1 +0`

### **E1.5**

Nếu phải dành nhiều thời gian để trở thành người rất giỏi trong một kỹ năng chuyên môn:

**A.** Tôi thấy đó là khoản đầu tư đáng giá. → `E1 +2`  
 **B.** Tôi sẽ làm nếu kỹ năng đó thực sự cần thiết. → `E1 +1`  
 **C.** Tôi thích phát triển nhiều năng lực khác nhau hơn. → `E1 +0`

# **E2 \- COGNITIVE EXPERTISE**

### **Chuyên môn tư duy & giải quyết vấn đề**

### **E2.1**

Khi gặp một vấn đề khó, bạn thường:

**A.** Phân tích nguyên nhân và tìm cấu trúc của vấn đề. → `E2 +2`  
 **B.** Tìm một vài cách giải quyết rồi so sánh. → `E2 +1`  
 **C.** Hỏi người có kinh nghiệm để tìm hướng xử lý. → `E2 +0`

### **E2.2**

Bạn thích nhiệm vụ nào nhất?

**A.** Một vấn đề chưa có đáp án rõ ràng và cần tự tìm lời giải. → `E2 +2`  
 **B.** Một vấn đề có nhiều phương án để lựa chọn. → `E2 +1`  
 **C.** Một nhiệm vụ có quy trình và đáp án tương đối rõ. → `E2 +0`

### **E2.3**

Khi hai nguồn thông tin đưa ra kết luận khác nhau, bạn thường:

**A.** Kiểm tra bằng chứng và tự đánh giá logic của từng bên. → `E2 +2`  
 **B.** So sánh các nguồn rồi chọn phương án đáng tin hơn. → `E2 +1`  
 **C.** Tin vào nguồn mà mình quen thuộc hoặc có chuyên môn hơn. → `E2 +0`

### **E2.4**

Trong một quyết định quan trọng, bạn ưu tiên:

**A.** Phân tích dữ liệu, nguyên nhân và hệ quả. → `E2 +2`  
 **B.** Kết hợp thông tin với kinh nghiệm và trực giác. → `E2 +1`  
 **C.** Hỏi ý kiến những người liên quan trước khi quyết định. → `E2 +0`

### **E2.5**

Điều nào khiến bạn thấy một nhiệm vụ thú vị?

**A.** Nó khiến tôi phải suy nghĩ theo một cách chưa từng thử. → `E2 +2`  
 **B.** Nó yêu cầu tôi kết hợp nhiều thông tin. → `E2 +1`  
 **C.** Nó cho phép tôi áp dụng một cách làm mà mình đã thành thạo. → `E2 +0`

# **E3 \- INTERPERSONAL EXPERTISE**

### **Chuyên môn giao tiếp & kết nối**

### **E3.1**

Khi làm việc nhóm, bạn thường mạnh nhất ở việc:

**A.** Kết nối mọi người và giúp nhóm phối hợp hiệu quả. → `E3 +2`  
 **B.** Đóng góp ý tưởng và hỗ trợ các thành viên khác. → `E3 +1`  
 **C.** Tập trung hoàn thành phần việc của mình. → `E3 +0`

### **E3.2**

Khi hai người trong nhóm bất đồng, bạn có xu hướng:

**A.** Tìm hiểu quan điểm của cả hai và giúp họ tìm điểm chung. → `E3 +2`  
 **B.** Đưa ra ý kiến để nhóm cân nhắc. → `E3 +1`  
 **C.** Để họ tự giải quyết nếu vấn đề không liên quan trực tiếp đến mình. → `E3 +0`

### **E3.3**

Bạn cảm thấy tự tin nhất khi:

**A.** Thuyết phục hoặc dẫn dắt người khác cùng hướng tới một mục tiêu. → `E3 +2`  
 **B.** Trình bày rõ ý tưởng của mình cho người khác. → `E3 +1`  
 **C.** Làm tốt công việc mà không cần phải tương tác quá nhiều. → `E3 +0`

### **E3.4**

Một nhiệm vụ yêu cầu bạn thường xuyên gặp gỡ, trao đổi và xây dựng quan hệ với nhiều người khiến bạn:

**A.** Khá hứng thú. → `E3 +2`  
 **B.** Có thể làm tốt nếu cần. → `E3 +1`  
 **C.** Thích những nhiệm vụ ít tương tác hơn. → `E3 +0`

### **E3.5**

Khi cần thuyết phục một người không đồng ý với mình, bạn thường:

**A.** Tìm hiểu điều họ quan tâm rồi điều chỉnh cách tiếp cận. → `E3 +2`  
 **B.** Trình bày rõ lý do và đưa ra bằng chứng. → `E3 +1`  
 **C.** Giữ quan điểm của mình và để họ tự quyết định. → `E3 +0`

# 

#  **S: SOCIAL ROLE**

### **Mục đích**

S đo **cách một cá nhân có xu hướng đảm nhận vai trò khi làm việc và tương tác với người khác**.

S trả lời câu hỏi:

> **“Khi làm việc với người khác, tôi có xu hướng đóng vai trò gì?”**

S **không đo tính cách hướng nội/hướng ngoại** và **không đo năng lực giao tiếp**. S tập trung vào **vai trò chức năng mà cá nhân thường đảm nhận trong một nhóm**.

Cơ sở lý thuyết có thể dựa trên **Functional/Team Roles**, đặc biệt là phân loại **Task Roles, Maintenance Roles và Individual Roles** của **Benne & Sheats (1948)**.

# **1\. Cấu trúc S**

S gồm **3 Social Role chính**, bên trong mỗi nhóm có các **sub-roles**.

| Mã | Social Role chính | Sub-roles |
| ----- | ----- | ----- |
| **S1** | **Task-oriented Roles** | Initiator, Information Giver, Coordinator, Evaluator-Critic |
| **S2** | **Maintenance / Social Roles** | Encourager/Energizer, Harmonizer/Mediator, Gatekeeper |
| **S3** | **Individual-oriented Roles** | Dominator, Recognition Seeker, Independent Contributor |

### **S1 — Task-oriented Roles**

Tập trung vào **hoàn thành nhiệm vụ, giải quyết vấn đề và đạt mục tiêu chung**.

* **Initiator:** Đề xuất ý tưởng hoặc hướng đi mới.  
* **Information Giver:** Cung cấp thông tin, kiến thức và kinh nghiệm.  
* **Coordinator:** Kết nối và điều phối các phần việc.  
* **Evaluator-Critic:** Phân tích, phản biện và đánh giá chất lượng.

### **S2 — Maintenance / Social Roles**

Tập trung vào **duy trì quan hệ, động lực và sự phối hợp giữa các thành viên**.

* **Encourager / Energizer:** Khích lệ và tạo động lực.  
* **Harmonizer / Mediator:** Hòa giải và giảm xung đột.  
* **Gatekeeper:** Điều tiết sự tham gia, tạo cơ hội để các thành viên lên tiếng.

### **S3 — Individual-oriented Roles**

Tập trung vào **cách cá nhân thể hiện vị trí và nhu cầu của mình trong nhóm**.

* **Dominator:** Có xu hướng kiểm soát hoặc áp đặt hướng đi.  
* **Recognition Seeker:** Tìm kiếm sự công nhận cho đóng góp cá nhân.  
* **Independent Contributor:** Ưu tiên tự chủ và đóng góp độc lập.

> **Lưu ý:** S3 không nên được diễn giải như một nhóm “tiêu cực”. Các sub-role chỉ mô tả **xu hướng hành vi**, không phải đánh giá đạo đức hay tính cách.

# **2\. Cấu trúc bài test**

S gồm **20 câu hỏi**.

* Mỗi sub-role có **2 câu**.  
* Mỗi câu có **3 lựa chọn**.  
* Mỗi lựa chọn tương ứng với mức độ thể hiện sub-role:  
  * Cao → `+2`  
  * Trung gian → `+1`  
  * Thấp → `+0`

Tổng:

> **10 sub-roles × 2 câu \= 20 câu**

# **3\. Logic chấm điểm**

Mỗi câu chỉ đóng góp điểm cho **một sub-role**.

Ví dụ:

### **Câu đo Coordinator**

**A.** Chủ động sắp xếp và kết nối các phần việc để mọi người phối hợp. → `Coordinator +2`

**B.** Phối hợp với các thành viên khi cần. → `Coordinator +1`

**C.** Chỉ tập trung hoàn thành phần việc của mình. → `Coordinator +0`

# **4\. Bộ 20 câu**

## **S1 — TASK-ORIENTED ROLES**

### **S1.1 — Initiator**

Khi nhóm bắt đầu một dự án nhưng chưa biết nên làm theo hướng nào, bạn thường:

**A.** Đề xuất một vài hướng mới để nhóm bắt đầu thảo luận. → `Initiator +2`  
**B.** Chờ mọi người đưa ra ý tưởng rồi đóng góp thêm nếu cần. → `Initiator +1`  
**C.** Chờ nhóm thống nhất hướng đi rồi mới bắt đầu phần việc của mình. → `Initiator +0`

### **S1.2 — Initiator**

Nếu bạn nhận thấy cách nhóm đang làm chưa hiệu quả, bạn sẽ:

**A.** Chủ động đề xuất một cách tiếp cận khác. → `Initiator +2`  
**B.** Gợi ý thay đổi một vài điểm trong cách làm hiện tại. → `Initiator +1`  
**C.** Tiếp tục làm theo cách nhóm đã thống nhất. → `Initiator +0`

### **S1.3 — Information Giver**

Khi nhóm đang thảo luận một vấn đề mà bạn có kiến thức liên quan, bạn thường:

**A.** Chủ động cung cấp thông tin và giải thích những điểm quan trọng. → `Information Giver +2`  
**B.** Chia sẻ thông tin khi có người hỏi hoặc khi thấy cần thiết. → `Information Giver +1`  
**C.** Để người khác tự tìm hiểu nếu vấn đề không thuộc phần việc của mình. → `Information Giver +0`

### **S1.4 — Information Giver**

Khi nhóm thiếu dữ liệu để đưa ra quyết định, bạn có xu hướng:

**A.** Tìm kiếm và cung cấp thông tin cần thiết cho cả nhóm. → `Information Giver +2`  
**B.** Tìm thông tin liên quan đến phần việc của mình. → `Information Giver +1`  
**C.** Chờ người phụ trách thông tin giải quyết vấn đề. → `Information Giver +0`

### **S1.5 — Coordinator**

Khi một dự án có nhiều người cùng tham gia, bạn thường:

**A.** Chủ động phân chia và kết nối các phần việc để mọi người phối hợp. → `Coordinator +2`  
**B.** Theo dõi phần việc của mình và phối hợp khi cần. → `Coordinator +1`  
**C.** Tập trung hoàn thành phần việc được giao. → `Coordinator +0`

### **S1.6 — Coordinator**

Nếu một thành viên hoàn thành công việc nhưng phần việc đó chưa kết nối được với phần của những người khác, bạn sẽ:

**A.** Tìm cách sắp xếp lại các phần việc để chúng liên kết với nhau. → `Coordinator +2`  
**B.** Trao đổi với những người liên quan để tìm cách phối hợp. → `Coordinator +1`  
**C.** Tiếp tục phần việc của mình và để người phụ trách xử lý. → `Coordinator +0`

### **S1.7 — Evaluator-Critic**

Khi nhóm đưa ra một ý tưởng mới, điều bạn thường chú ý nhất là:

**A.** Kiểm tra xem ý tưởng có hợp lý, khả thi và hiệu quả hay không. → `Evaluator-Critic +2`  
**B.** Cân nhắc ưu điểm và hạn chế của ý tưởng. → `Evaluator-Critic +1`  
**C.** Quan tâm nhiều hơn đến việc mọi người có thích ý tưởng đó hay không. → `Evaluator-Critic +0`

### **S1.8 — Evaluator-Critic**

Khi xem lại sản phẩm của nhóm trước khi nộp, bạn thường:

**A.** Tìm những lỗi, điểm thiếu logic hoặc điểm có thể cải thiện. → `Evaluator-Critic +2`  
**B.** Kiểm tra những lỗi rõ ràng và những yêu cầu chính. → `Evaluator-Critic +1`  
**C.** Tin rằng phần việc đã hoàn thành thì không cần kiểm tra quá nhiều. → `Evaluator-Critic +0`

# **S2 — MAINTENANCE / SOCIAL ROLES**

### **S2.1 — Encourager / Energizer**

Khi nhóm bắt đầu mất động lực vì công việc khó, bạn thường:

**A.** Chủ động động viên và giúp mọi người lấy lại tinh thần. → `Encourager +2`  
**B.** Động viên một vài thành viên nếu thấy họ thực sự cần. → `Encourager +1`  
**C.** Tập trung vào phần việc của mình. → `Encourager +0`

### **S2.2 — Encourager / Energizer**

Khi một thành viên làm tốt một phần việc, bạn thường:

**A.** Chủ động ghi nhận và khích lệ họ. → `Encourager +2`  
**B.** Ghi nhận nếu thành tích đó thực sự nổi bật. → `Encourager +1`  
**C.** Cho rằng hoàn thành tốt công việc là điều bình thường. → `Encourager +0`

### **S2.3 — Harmonizer / Mediator**

Khi hai thành viên trong nhóm xảy ra bất đồng, bạn thường:

**A.** Tìm hiểu quan điểm của cả hai và giúp họ tìm điểm chung. → `Harmonizer +2`  
**B.** Đưa ra ý kiến nếu cuộc tranh luận bắt đầu ảnh hưởng đến nhóm. → `Harmonizer +1`  
**C.** Để hai người tự giải quyết vấn đề của họ. → `Harmonizer +0`

### **S2.4 — Harmonizer / Mediator**

Nếu một cuộc tranh luận trong nhóm trở nên căng thẳng, bạn sẽ:

**A.** Chủ động làm dịu tình hình và đưa cuộc thảo luận trở lại vấn đề. → `Harmonizer +2`  
**B.** Chờ mọi người bình tĩnh rồi tiếp tục thảo luận. → `Harmonizer +1`  
**C.** Không can thiệp nếu vấn đề không trực tiếp liên quan đến mình. → `Harmonizer +0`

### **S2.5 — Gatekeeper**

Trong một cuộc họp, bạn nhận thấy một thành viên gần như không có cơ hội phát biểu. Bạn sẽ:

**A.** Chủ động mời họ chia sẻ quan điểm. → `Gatekeeper +2`  
**B.** Chờ xem họ có muốn tham gia hay không. → `Gatekeeper +1`  
**C.** Tiếp tục cuộc thảo luận vì họ có thể tự lên tiếng nếu muốn. → `Gatekeeper +0`

### **S2.6 — Gatekeeper**

Khi một người liên tục nói và khiến những người khác khó tham gia, bạn thường:

**A.** Chủ động điều tiết cuộc trò chuyện để tạo cơ hội cho những người khác. → `Gatekeeper +2`  
**B.** Nhắc nhẹ nếu cuộc trò chuyện bắt đầu mất cân bằng. → `Gatekeeper +1`  
**C.** Để người dẫn cuộc họp tự xử lý. → `Gatekeeper +0`

# **S3 — INDIVIDUAL-ORIENTED ROLES**

### **S3.1 — Dominator**

Khi nhóm không thống nhất được cách làm, bạn thường:

**A.** Chủ động quyết định hướng đi và yêu cầu mọi người thực hiện. → `Dominator +2`  
**B.** Đưa ra quan điểm mạnh và cố gắng thuyết phục nhóm theo hướng đó. → `Dominator +1`  
**C.** Chờ nhóm thảo luận và cùng thống nhất quyết định. → `Dominator +0`

### **S3.2 — Dominator**

Nếu bạn tin rằng phương án của mình tốt nhất nhưng nhóm đang chọn phương án khác:

**A.** Tôi sẽ cố gắng giành quyền quyết định để nhóm làm theo phương án của mình. → `Dominator +2`  
**B.** Tôi sẽ cố gắng thuyết phục nhóm thêm một lần nữa. → `Dominator +1`  
**C.** Tôi chấp nhận quyết định chung nếu nhóm đã thống nhất. → `Dominator +0`

### **S3.3 — Recognition Seeker**

Sau khi hoàn thành một nhiệm vụ quan trọng, điều khiến bạn hài lòng nhất là:

**A.** Mọi người biết và ghi nhận đóng góp của tôi. → `Recognition Seeker +2`  
**B.** Được ghi nhận nếu tôi thực sự làm tốt. → `Recognition Seeker +1`  
**C.** Biết mình đã hoàn thành tốt là đủ. → `Recognition Seeker +0`

### **S3.4 — Recognition Seeker**

Nếu một ý tưởng của bạn đóng góp lớn vào thành công của nhóm nhưng không ai biết đó là ý tưởng của bạn:

**A.** Tôi sẽ muốn mọi người biết đóng góp đó là của mình. → `Recognition Seeker +2`  
**B.** Tôi hơi tiếc nhưng vẫn chấp nhận được. → `Recognition Seeker +1`  
**C.** Tôi không quan tâm miễn là nhóm đạt kết quả tốt. → `Recognition Seeker +0`

### **S3.5 — Independent Contributor**

Khi được giao một phần việc lớn trong dự án, bạn thích:

**A.** Được tự quyết định cách làm và hoàn thành phần việc độc lập. → `Independent Contributor +2`  
**B.** Có một số trao đổi với nhóm nhưng vẫn tự xử lý phần lớn công việc. → `Independent Contributor +1`  
**C.** Làm việc sát với các thành viên khác trong suốt quá trình. → `Independent Contributor +0`

### **S3.6 — Independent Contributor**

Nếu có thể lựa chọn giữa:

**A.** Một nhiệm vụ bạn có thể tự chịu trách nhiệm từ đầu đến cuối. → `Independent Contributor +2`  
**B.** Một nhiệm vụ kết hợp giữa làm độc lập và phối hợp. → `Independent Contributor +1`  
**C.** Một nhiệm vụ yêu cầu liên tục trao đổi và phối hợp với người khác. → `Independent Contributor +0`

# **5\. Logic tổng hợp kết quả**

Đây là phần **quan trọng nhất để IT implement**.

### **Bước 1 \- Tính điểm sub-role**

Mỗi sub-role có:

> **2 câu × 2 điểm \= tối đa 4 điểm**

Ví dụ:

Initiator \= 3/4

Information Giver \= 2/4

Coordinator \= 4/4

Evaluator-Critic \= 3/4

### **Bước 2 — Tính điểm của 3 Social Roles chính**

**Không cộng điểm thô**, vì S1 có 4 sub-role trong khi S2 và S3 chỉ có 3\.

Tính:

> **S1 \= (Initiator \+ Information Giver \+ Coordinator \+ Evaluator-Critic) / 16 × 100**

> **S2 \= (Encourager \+ Harmonizer \+ Gatekeeper) / 12 × 100**

> **S3 \= (Dominator \+ Recognition Seeker \+ Independent Contributor) / 12 × 100**

Ví dụ:

| Sub-role | Score |
| ----- | ----- |
| Initiator | 3/4 |
| Information Giver | 2/4 |
| Coordinator | 4/4 |
| Evaluator-Critic | 3/4 |
| Encourager | 2/4 |
| Harmonizer | 3/4 |
| Gatekeeper | 2/4 |
| Dominator | 1/4 |
| Recognition Seeker | 2/4 |
| Independent Contributor | 3/4 |

→

**S1 \= 12/16 \= 75%**

**S2 \= 7/12 \= 58.3%**

**S3 \= 6/12 \= 50%**

→ **Kết luận: S1 \- Task-oriented Role**

### **Bước 3 — Xác định sub-role nổi trội**

Sau khi xác định S chính, hệ thống tìm **sub-role có điểm cao nhất trong S đó**.

Ví dụ:

> **Primary Social Role: S1 \- Task-oriented**  
> **Dominant Sub-role: Coordinator**

→ AI mô tả người chơi là người có xu hướng **đưa công việc tiến về phía trước thông qua việc kết nối và điều phối các thành viên**.

# **6\. Output cho người chơi**

Ví dụ:

### **Your Social Role**

**S1 — Task-oriented Role**

> Bạn có xu hướng tập trung vào việc **đưa nhóm đạt mục tiêu, giải quyết vấn đề và tổ chức công việc**.

**Your dominant sub-role: Coordinator**

> Bạn đặc biệt có xu hướng kết nối các phần việc và giúp các thành viên phối hợp để công việc vận hành thống nhất.

### **Social Role Profile**

| Role | Score |
| ----- | ----- |
| **S1 — Task-oriented** | **75%** |
| S2 — Maintenance / Social | 58% |
| S3 — Individual-oriented | 50% |

### **Sub-role Profile**

| Sub-role | Score |
| ----- | ----- |
| Coordinator | 100% |
| Initiator | 75% |
| Evaluator-Critic | 75% |
| Information Giver | 50% |
|  |  |

**Như vậy, kết quả cuối cùng của S chỉ có 3 hướng chính: S1 / S2 / S3.** Các sub-role không phải kết quả độc lập mà dùng để **giải thích người chơi thuộc S1/S2/S3 theo kiểu nào**.

# **M \- MIND**

### **Dựa vào nền tảng lý thuyết**

* **Cognitive Information Processing (CIP) Theory:** Sampson, Lenz, Reardon & Peterson (1999), tập trung vào cách cá nhân **xử lý thông tin, giải quyết vấn đề và ra quyết định trong bối cảnh nghề nghiệp**.  
* **Career Decision-Making:** nhấn mạnh quá trình cá nhân tiếp nhận thông tin, xác định lựa chọn, đánh giá các phương án và đưa ra quyết định nghề nghiệp.

M **không đo kiến thức, IQ hay năng lực chuyên môn**. M tập trung vào **quá trình tư duy và cách người chơi tiếp cận thông tin trước khi hành động**.

### **Gồm 3 nhóm:**

* **M1 — Information Processing:** Cách tiếp nhận, chọn lọc và đánh giá thông tin  
* **M2 — Cognitive Flexibility:** Khả năng thay đổi góc nhìn và điều chỉnh cách suy nghĩ  
* **M3 — Decision Making:** Cách cân nhắc và đưa ra quyết định

**Mỗi nhóm 5 câu. Mỗi câu 3 lựa chọn.**

# **M1 \- INFORMATION PROCESSING**

### **Xử lý thông tin**

### **M1.1**

Khi nhận được một lượng lớn thông tin về một vấn đề, bạn thường:

**A.** Xác định những thông tin quan trọng nhất trước. → `M1 +2`  
 **B.** Đọc qua toàn bộ rồi mới quyết định thông tin nào cần chú ý. → `M1 +1`  
 **C.** Tập trung vào thông tin dễ hiểu hoặc quen thuộc nhất. → `M1 +0`

### **M1.2**

Khi hai nguồn thông tin đưa ra kết luận khác nhau, bạn thường:

**A.** Kiểm tra bằng chứng và cách mỗi bên đưa ra kết luận. → `M1 +2`  
 **B.** So sánh hai nguồn rồi chọn nguồn đáng tin hơn. → `M1 +1`  
 **C.** Tin vào nguồn mà bạn quen thuộc hoặc thường sử dụng. → `M1 +0`

### **M1.3**

Khi phải tìm hiểu một vấn đề bạn chưa biết, bạn thường:

**A.** Tìm nhiều góc nhìn khác nhau trước khi hình thành kết luận. → `M1 +2`  
 **B.** Tìm một nguồn đáng tin rồi dựa vào đó để hiểu vấn đề. → `M1 +1`  
 **C.** Tìm câu trả lời nhanh nhất có thể. → `M1 +0`

### **M1.4**

Khi một thông tin mới mâu thuẫn với điều bạn đã tin trước đó, bạn thường:

**A.** Kiểm tra lại cả thông tin cũ và mới trước khi kết luận. → `M1 +2`  
 **B.** Xem xét thông tin mới nhưng vẫn giữ quan điểm ban đầu nếu chưa chắc chắn. → `M1 +1`  
 **C.** Bỏ qua thông tin mới nếu nó không phù hợp với suy nghĩ của bạn. → `M1 +0`

### **M1.5**

Khi phải đưa ra kết luận từ nhiều thông tin chưa hoàn chỉnh, bạn thường:

**A.** Xác định thông tin còn thiếu trước khi kết luận. → `M1 +2`  
 **B.** Sử dụng những thông tin hiện có và thừa nhận rằng kết luận có thể thay đổi. → `M1 +1`  
 **C.** Chọn phương án có vẻ hợp lý nhất và quyết định luôn. → `M1 +0`

# **M2 \- COGNITIVE FLEXIBILITY**

### **Linh hoạt trong tư duy**

### **M2.1**

Khi cách tiếp cận ban đầu không mang lại kết quả, bạn thường:

**A.** Xem xét lại giả định ban đầu và thử một cách tiếp cận khác. → `M2 +2`  
 **B.** Điều chỉnh một vài điểm trong cách làm hiện tại. → `M2 +1`  
 **C.** Tiếp tục với cách ban đầu vì đã quen với nó. → `M2 +0`

### **M2.2**

Khi một người đưa ra quan điểm hoàn toàn trái ngược với bạn, bạn thường:

**A.** Cố gắng hiểu lý do khiến họ có quan điểm đó. → `M2 +2`  
 **B.** Nghe quan điểm của họ rồi so sánh với quan điểm của mình. → `M2 +1`  
 **C.** Giữ quan điểm của mình nếu bạn đã tin rằng nó đúng. → `M2 +0`

### **M2.3**

Khi một vấn đề có nhiều cách tiếp cận khác nhau, bạn thường:

**A.** Thử xem xét vấn đề từ nhiều góc độ trước khi chọn cách tiếp cận. → `M2 +2`  
 **B.** So sánh một vài cách tiếp cận rồi chọn cách phù hợp nhất. → `M2 +1`  
 **C.** Chọn cách bạn quen thuộc nhất. → `M2 +0`

### **M2.4**

Nếu phát hiện một quyết định trước đây của mình không còn phù hợp với tình hình hiện tại, bạn thường:

**A.** Sẵn sàng thay đổi quyết định dựa trên tình hình mới. → `M2 +2`  
 **B.** Cân nhắc lại nhưng chỉ thay đổi khi có bằng chứng rõ ràng. → `M2 +1`  
 **C.** Giữ quyết định cũ vì đã mất nhiều công sức để đưa ra nó. → `M2 +0`

### **M2.5**

Khi một tình huống bất ngờ xảy ra và kế hoạch ban đầu không còn phù hợp, bạn thường:

**A.** Nhanh chóng xây dựng một phương án mới dựa trên tình hình hiện tại. → `M2 +2`  
 **B.** Điều chỉnh kế hoạch ban đầu để phù hợp với tình hình. → `M2 +1`  
 **C.** Cố gắng thực hiện kế hoạch ban đầu nếu vẫn còn khả năng. → `M2 +0`

# **M3 \- DECISION MAKING**

### **Ra quyết định**

### **M3.1**

Khi phải lựa chọn giữa hai phương án đều có ưu điểm, bạn thường:

**A.** Xác định các tiêu chí quan trọng rồi so sánh từng phương án. → `M3 +2`  
 **B.** Cân nhắc ưu và nhược điểm của mỗi phương án. → `M3 +1`  
 **C.** Chọn phương án bạn cảm thấy phù hợp hơn. → `M3 +0`

### **M3.2**

Khi phải đưa ra quyết định trong thời gian ngắn, bạn thường:

**A.** Xác định yếu tố quan trọng nhất rồi quyết định dựa trên nó. → `M3 +2`  
 **B.** Cân nhắc nhanh một vài ưu và nhược điểm chính. → `M3 +1`  
 **C.** Chọn phương án đầu tiên có vẻ hợp lý. → `M3 +0`

### **M3.3**

Khi một lựa chọn có lợi ích lớn nhưng cũng có rủi ro cao, bạn thường:

**A.** Đánh giá khả năng xảy ra và mức độ ảnh hưởng của rủi ro trước khi quyết định. → `M3 +2`  
 **B.** Cân nhắc lợi ích và rủi ro rồi lựa chọn phương án phù hợp. → `M3 +1`  
 **C.** Tránh lựa chọn vì không muốn gặp rủi ro. → `M3 +0`

### **M3.4**

Khi quyết định của bạn ảnh hưởng đến nhiều người khác, bạn thường:

**A.** Cân nhắc lợi ích và tác động đối với các bên liên quan. → `M3 +2`  
 **B.** Hỏi ý kiến những người bị ảnh hưởng trước khi quyết định. → `M3 +1`  
 **C.** Ưu tiên phương án mà bạn cho là tốt nhất. → `M3 +0`

### **M3.5**

Sau khi đưa ra một quyết định quan trọng, bạn thường:

**A.** Theo dõi kết quả và đánh giá xem quyết định có hiệu quả không. → `M3 +2`  
 **B.** Xem xét lại nếu kết quả không như mong đợi. → `M3 +1`  
 **C.** Chuyển sang việc khác sau khi quyết định đã được đưa ra. → `M3 +0`

# **Logic tổng hợp kết quả**

Mỗi nhóm có:

> **5 câu × 2 điểm \= tối đa 10 điểm**

### **Bước 1 — Tính điểm 3 nhóm**

> **M1 \= điểm M1 / 10 × 100**

> **M2 \= điểm M2 / 10 × 100**

> **M3 \= điểm M3 / 10 × 100**

Ví dụ:

| Dimension | Score |
| ----- | ----- |
| M1 — Information Processing | 80% |
| M2 — Cognitive Flexibility | 60% |
| M3 — Decision Making | 80% |

# **A — ADAPTABILITY**

### **“Tôi thích nghi với thay đổi như thế nào?”**

### **Dựa vào nền tảng lý thuyết**

**Career Construction Theory** — Savickas (2005) và **Career Adapt-Abilities Scale (CAAS)** — Savickas & Porfeli (2012).

Career Adaptability được xem là nguồn lực giúp cá nhân **đối phó với nhiệm vụ nghề nghiệp, chuyển tiếp và những thay đổi trong quá trình phát triển nghề nghiệp**.

CAAS gồm **4 dimensions**:

* **A1 — Concern:** Tôi có quan tâm và chuẩn bị cho tương lai không?  
* **A2 — Control:** Tôi có chủ động và chịu trách nhiệm với hướng đi của mình không?  
* **A3 — Curiosity:** Tôi có khám phá bản thân và những khả năng nghề nghiệp khác nhau không?  
* **A4 — Confidence:** Tôi có tin tưởng vào khả năng vượt qua khó khăn và giải quyết vấn đề của mình không?

> **Lưu ý:** DESMAP sử dụng 4 dimensions này làm **cơ sở lý thuyết để xây dựng nhóm A**, không đồng nghĩa DESMAP là CAAS hay sử dụng nguyên bản CAAS.

# **A1 — CONCERN**

### **Quan tâm & chuẩn bị cho tương lai**

**A1.1** Khi nghĩ về nghề nghiệp trong tương lai, bạn thường:

A. Chủ động tìm hiểu những khả năng mình có thể theo đuổi. → `A1 +2`  
 B. Nghĩ về tương lai nhưng chưa tìm hiểu cụ thể. → `A1 +1`  
 C. Chỉ bắt đầu quan tâm khi phải đưa ra lựa chọn. → `A1 +0`

**A1.2** Nếu ngành nghề bạn đang quan tâm có nhiều thay đổi trong tương lai, bạn thường:

A. Tìm hiểu trước những thay đổi có thể xảy ra. → `A1 +2`  
 B. Theo dõi thêm rồi mới quyết định có cần thay đổi không. → `A1 +1`  
 C. Chỉ quan tâm khi sự thay đổi thực sự xảy ra. → `A1 +0`

**A1.3** Khi lập kế hoạch cho tương lai, bạn thường:

A. Chuẩn bị nhiều khả năng khác nhau. → `A1 +2`  
 B. Có một kế hoạch chính và một vài phương án dự phòng. → `A1 +1`  
 C. Tập trung vào kế hoạch hiện tại. → `A1 +0`

# **A2 — CONTROL**

### **Chủ động & chịu trách nhiệm với hướng đi**

**A2.1** Nếu nhận ra lựa chọn nghề nghiệp hiện tại không còn phù hợp với mình, bạn thường:

A. Chủ động tìm hiểu và điều chỉnh hướng đi. → `A2 +2`  
 B. Cân nhắc một thời gian trước khi thay đổi. → `A2 +1`  
 C. Tiếp tục lựa chọn hiện tại vì đã bắt đầu rồi. → `A2 +0`

**A2.2** Khi phải đưa ra một quyết định quan trọng về tương lai, bạn thường:

A. Tự tìm hiểu và đưa ra quyết định dựa trên thông tin mình có. → `A2 +2`  
 B. Tham khảo ý kiến người khác rồi tự quyết định. → `A2 +1`  
 C. Chờ người khác định hướng cho mình. → `A2 +0`

**A2.3** Nếu kế hoạch nghề nghiệp của bạn không diễn ra như dự kiến, bạn thường:

A. Xác định mình có thể thay đổi điều gì để tiếp tục. → `A2 +2`  
 B. Điều chỉnh kế hoạch nếu tình hình không cải thiện. → `A2 +1`  
 C. Chờ xem tình hình sẽ tự thay đổi như thế nào. → `A2 +0`

# **A3 — CURIOSITY**

### **Khám phá bản thân & những khả năng mới**

**A3.1** Nếu được giới thiệu một nghề mà trước đây bạn chưa từng nghĩ đến, bạn thường:

A. Tìm hiểu xem nghề đó có phù hợp với mình không. → `A3 +2`  
 B. Nghe thêm thông tin nhưng chưa chắc sẽ tìm hiểu sâu. → `A3 +1`  
 C. Bỏ qua vì đó không phải nghề mình đang quan tâm. → `A3 +0`

**A3.2** Khi phát hiện một lĩnh vực mới có thể phù hợp với năng lực của mình, bạn thường:

A. Chủ động tìm hiểu và thử trải nghiệm nó. → `A3 +2`  
 B. Tìm hiểu thêm trước khi quyết định có thử hay không. → `A3 +1`  
 C. Tiếp tục tập trung vào lĩnh vực ban đầu. → `A3 +0`

**A3.3** Khi tìm hiểu về một nghề, bạn thường muốn biết:

A. Nhiều khía cạnh khác nhau của nghề, kể cả những điều mình chưa biết. → `A3 +2`  
 B. Những thông tin cần thiết để quyết định có phù hợp hay không. → `A3 +1`  
 C. Chủ yếu những thông tin xác nhận rằng nghề đó phù hợp với mình. → `A3 +0`

# **A4 — CONFIDENCE**

### **Tin tưởng vào khả năng của bản thân**

**A4.1** Nếu phải học một kỹ năng hoàn toàn mới để theo đuổi một hướng nghề nghiệp, bạn thường:

A. Tin rằng mình có thể học được nếu dành đủ thời gian và nỗ lực. → `A4 +2`  
 B. Thử học trước rồi đánh giá khả năng của mình. → `A4 +1`  
 C. Chỉ muốn theo hướng đó nếu mình đã có sẵn kỹ năng cần thiết. → `A4 +0`

**A4.2** Khi bước vào một môi trường hoàn toàn mới, bạn thường:

A. Chủ động quan sát, học hỏi và điều chỉnh để hòa nhập. → `A4 +2`  
 B. Cần một khoảng thời gian để làm quen rồi mới chủ động hơn. → `A4 +1`  
 C. Khó bắt đầu nếu chưa biết rõ mình phải làm gì. → `A4 +0`

**A4.3** Nếu một lựa chọn nghề nghiệp đòi hỏi bạn phải thay đổi cách làm quen thuộc, bạn thường:

A. Sẵn sàng thử cách mới và điều chỉnh trong quá trình thực hiện. → `A4 +2`  
 B. Thử thay đổi nếu thấy cách cũ không còn hiệu quả. → `A4 +1`  
 C. Ưu tiên giữ cách làm mình đã quen. → `A4 +0`

# **Tổng hợp điểm A**

| Dimension | Nội dung | Số câu | Điểm tối đa |
| ----- | ----- | ----- | ----- |
| **A1 — Concern** | Quan tâm & chuẩn bị cho tương lai | 3 | 6 |
| **A2 — Control** | Chủ động & kiểm soát hướng đi | 3 | 6 |
| **A3 — Curiosity** | Khám phá khả năng mới | 3 | 6 |
| **A4 — Confidence** | Tin tưởng vào khả năng bản thân | 3 | 6 |
| **A — Adaptability** | **Tổng** | **12** | **24** |

**Điểm A \= (Tổng điểm đạt được / 24\) × 100**

AI đồng thời lưu điểm của từng dimension để tạo **Adaptability Profile**, thay vì chỉ đưa ra một điểm tổng.

### **Ví dụ:**

> **Adaptability — 79%**  
>  Concern — 83%  
>  Control — 67%  
>  Curiosity — 84%  
>  Confidence — 82%

→ AI có thể diễn giải rằng người dùng **có xu hướng chủ động khám phá và chuẩn bị cho tương lai, khá tự tin khi đối mặt với thay đổi, nhưng mức độ chủ động kiểm soát hướng đi vẫn có thể được cải thiện.**

### **Vai trò của A trong DESMAP**

A bổ sung một khía cạnh mà **hồ sơ tĩnh** như sở thích, năng lực hay tính cách khó phản ánh đầy đủ:

> **“Khi nghề nghiệp và môi trường thay đổi, tôi có khả năng thích nghi với thay đổi đó như thế nào?”**

# **P — PRESSURE**

### **“Tôi phản ứng như thế nào khi công việc trở nên áp lực?”**

### **Mục đích**

P đo **cách một cá nhân phản ứng khi phải đối mặt với các yêu cầu, áp lực và tình huống căng thẳng trong môi trường nghề nghiệp**.

P trả lời câu hỏi:

> **“Khi công việc trở nên áp lực, tôi phản ứng và duy trì hiệu quả như thế nào?”**

P **không đo mức độ stress hay sức khỏe tâm lý**. P tập trung vào **phản ứng trước các dạng áp lực có thể xuất hiện trong công việc**.

Cơ sở lý thuyết chính có thể tham khảo:

* **COPSOQ III** — Copenhagen Psychosocial Questionnaire: các dạng job demands như quantitative demands, work pace, cognitive demands, emotional demands.  
* **Challenge–Hindrance Stressor Framework** — LePine, Podsakoff & LePine (2005): phân biệt các dạng stressor/challenge trong công việc.

> **Lưu ý:** Các dimension dưới đây là cấu trúc được **xây dựng cho DEPMAP dựa trên các nhóm work demands/stressors trong nghiên cứu**, không phải nguyên bản một thang đo có sẵn.

# **1\. Cấu trúc P**

P gồm **6 dimensions**, mỗi nhóm **3 câu**, tổng cộng **18 câu**.

| Mã | Dimension | Nội dung |
| ----- | ----- | ----- |
| **P1** | Time & Pace Pressure | Áp lực thời gian và tốc độ |
| **P2** | Workload Pressure | Áp lực khối lượng công việc |
| **P3** | Cognitive Pressure | Áp lực tư duy, quyết định |
| **P4** | Emotional Pressure | Áp lực kiểm soát cảm xúc |
| **P5** | Interpersonal Pressure | Áp lực từ tương tác và xung đột |
| **P6** | Responsibility Pressure | Áp lực từ trách nhiệm và hậu quả |

# **2\. Logic chấm điểm**

Mỗi câu có **3 lựa chọn**.

* Phản ứng thể hiện **khả năng duy trì hiệu quả tốt dưới áp lực** → `+2`  
* Phản ứng **trung gian** → `+1`  
* Phản ứng cho thấy **dễ bị ảnh hưởng bởi áp lực** → `+0`

Mỗi dimension có 3 câu:

> **Điểm tối đa mỗi P \= 6**

Quy đổi:

| Điểm | % |
| ----- | ----- |
| 0/6 | 0% |
| 1/6 | 17% |
| 2/6 | 33% |
| 3/6 | 50% |
| 4/6 | 67% |
| 5/6 | 83% |
| 6/6 | 100% |

# **3\. BỘ 18 CÂU**

## **P1 — TIME & PACE PRESSURE**

### **Áp lực thời gian và tốc độ**

### **P1.1**

Bạn đang làm một nhiệm vụ quan trọng nhưng thời gian còn lại chỉ bằng một nửa dự kiến. Bạn sẽ:

**A.** Xác định phần quan trọng nhất và tập trung hoàn thành trước. → `P1 +2`  
 **B.** Tăng tốc độ làm việc và cố gắng hoàn thành toàn bộ. → `P1 +1`  
 **C.** Khó tập trung vì quá lo lắng về thời gian. → `P1 +0`

### **P1.2**

Bạn phải hoàn thành nhiều việc trong một khoảng thời gian rất ngắn. Bạn thường:

**A.** Sắp xếp thứ tự ưu tiên rồi xử lý từng việc. → `P1 +2`  
 **B.** Làm nhiều việc cùng lúc để tiết kiệm thời gian. → `P1 +1`  
 **C.** Dễ bị rối vì có quá nhiều việc phải hoàn thành. → `P1 +0`

### **P1.3**

Khi phải làm việc với tốc độ cao trong thời gian dài:

**A.** Tôi có thể duy trì tốc độ bằng cách điều chỉnh cách làm. → `P1 +2`  
 **B.** Tôi vẫn cố gắng duy trì nhưng hiệu suất có thể giảm. → `P1 +1`  
 **C.** Tôi nhanh chóng mất tập trung và khó duy trì hiệu suất. → `P1 +0`

# **P2 — WORKLOAD PRESSURE**

### **Áp lực khối lượng công việc**

### **P2.1**

Bạn được giao nhiều nhiệm vụ cùng lúc và tất cả đều quan trọng. Bạn sẽ:

**A.** Phân loại mức độ ưu tiên và xây dựng cách xử lý. → `P2 +2`  
 **B.** Bắt đầu từ nhiệm vụ dễ hoặc quen thuộc nhất. → `P2 +1`  
 **C.** Cảm thấy quá tải vì không biết nên bắt đầu từ đâu. → `P2 +0`

### **P2.2**

Nếu khối lượng công việc tăng đột ngột, bạn thường:

**A.** Điều chỉnh kế hoạch và phân bổ lại thời gian. → `P2 +2`  
 **B.** Cố gắng làm nhiều hơn để theo kịp. → `P2 +1`  
 **C.** Dễ mất kiểm soát vì lượng công việc quá lớn. → `P2 +0`

### **P2.3**

Khi phải duy trì khối lượng công việc cao trong nhiều ngày:

**A.** Tôi điều chỉnh nhịp làm việc để duy trì hiệu quả. → `P2 +2`  
 **B.** Tôi vẫn hoàn thành nhưng cần nhiều thời gian nghỉ hơn. → `P2 +1`  
 **C.** Chất lượng công việc của tôi giảm rõ rệt. → `P2 +0`

# **P3 — COGNITIVE PRESSURE**

### **Áp lực tư duy và ra quyết định**

### **P3.1**

Bạn phải đưa ra một quyết định quan trọng trong khi chưa có đầy đủ thông tin. Bạn sẽ:

**A.** Xác định thông tin quan trọng nhất và đưa ra quyết định dựa trên những gì có thể kiểm chứng. → `P3 +2`  
 **B.** Cân nhắc các khả năng rồi chọn phương án phù hợp nhất. → `P3 +1`  
 **C.** Khó quyết định vì sợ lựa chọn sai. → `P3 +0`

### **P3.2**

Khi một vấn đề trở nên phức tạp hơn nhiều so với dự kiến:

**A.** Tôi chia vấn đề thành từng phần để xử lý. → `P3 +2`  
 **B.** Tôi tìm thêm thông tin hoặc hỗ trợ trước khi tiếp tục. → `P3 +1`  
 **C.** Tôi dễ bị quá tải và không biết nên xử lý từ đâu. → `P3 +0`

### **P3.3**

Bạn phải đưa ra quyết định nhanh trong một tình huống có rủi ro. Bạn thường:

**A.** Xác định rủi ro lớn nhất và quyết định dựa trên yếu tố quan trọng nhất. → `P3 +2`  
 **B.** Cân nhắc nhanh ưu và nhược điểm trước khi chọn. → `P3 +1`  
 **C.** Khó đưa ra quyết định khi không có đủ thời gian suy nghĩ. → `P3 +0`

# **P4 — EMOTIONAL PRESSURE**

### **Áp lực cảm xúc**

### **P4.1**

Bạn đang rất căng thẳng nhưng vẫn phải tiếp tục làm việc. Bạn thường:

**A.** Tạm điều chỉnh cảm xúc rồi tập trung trở lại vào nhiệm vụ. → `P4 +2`  
 **B.** Vẫn tiếp tục làm nhưng hiệu suất có thể giảm. → `P4 +1`  
 **C.** Khó kiểm soát cảm xúc khiến tôi khó tiếp tục công việc. → `P4 +0`

### **P4.2**

Bạn vừa mắc một lỗi nghiêm trọng và biết rằng người khác đang chờ kết quả. Bạn sẽ:

**A.** Tập trung xác định lỗi và tìm cách khắc phục. → `P4 +2`  
 **B.** Cảm thấy lo lắng nhưng vẫn cố gắng sửa lỗi. → `P4 +1`  
 **C.** Quá lo lắng về hậu quả nên khó tập trung xử lý. → `P4 +0`

### **P4.3**

Bạn nhận được lời phê bình khá gay gắt ngay trước khi tiếp tục công việc. Bạn thường:

**A.** Tách cảm xúc khỏi vấn đề và tập trung vào điều cần cải thiện. → `P4 +2`  
 **B.** Cảm thấy khó chịu nhưng vẫn tiếp tục công việc. → `P4 +1`  
 **C.** Bị ảnh hưởng tâm lý và khó tập trung sau đó. → `P4 +0`

# **P5 — INTERPERSONAL PRESSURE**

### **Áp lực từ tương tác và xung đột**

### **P5.1**

Một người liên tục phản đối ý kiến của bạn trong khi công việc đang cần được giải quyết nhanh. Bạn sẽ:

**A.** Giữ bình tĩnh và tập trung tìm giải pháp cho vấn đề. → `P5 +2`  
 **B.** Bảo vệ quan điểm của mình nhưng vẫn cố tiếp tục công việc. → `P5 +1`  
 **C.** Cảm thấy căng thẳng và khó tiếp tục trao đổi. → `P5 +0`

### **P5.2**

Một khách hàng/đối tác đang rất không hài lòng với kết quả công việc. Bạn sẽ:

**A.** Lắng nghe vấn đề và tập trung tìm cách xử lý. → `P5 +2`  
 **B.** Giải thích tình hình và cố gắng tìm giải pháp phù hợp. → `P5 +1`  
 **C.** Cảm thấy áp lực và khó biết nên phản ứng thế nào. → `P5 +0`

### **P5.3**

Bạn phải làm việc với một người có phong cách hoàn toàn trái ngược với mình trong thời gian dài. Bạn thường:

**A.** Điều chỉnh cách giao tiếp để duy trì hiệu quả hợp tác. → `P5 +2`  
 **B.** Cố gắng hợp tác dù đôi lúc cảm thấy khó chịu. → `P5 +1`  
 **C.** Dễ bị ảnh hưởng bởi sự khác biệt và muốn hạn chế tương tác. → `P5 +0`

# **P6 — RESPONSIBILITY PRESSURE**

### **Áp lực trách nhiệm và hậu quả**

### **P6.1**

Bạn phải đưa ra một quyết định mà kết quả có thể ảnh hưởng trực tiếp đến người khác. Bạn sẽ:

**A.** Đánh giá thông tin, hậu quả và đưa ra quyết định có cơ sở. → `P6 +2`  
 **B.** Tham khảo ý kiến người khác trước khi quyết định. → `P6 +1`  
 **C.** Cảm thấy rất áp lực vì sợ mình đưa ra quyết định sai. → `P6 +0`

### **P6.2**

Bạn là người chịu trách nhiệm chính cho một nhiệm vụ quan trọng. Khi có vấn đề xảy ra:

**A.** Chủ động nhận trách nhiệm và tập trung giải quyết vấn đề. → `P6 +2`  
 **B.** Tìm sự hỗ trợ từ những người liên quan rồi xử lý. → `P6 +1`  
 **C.** Dễ hoang mang vì cảm thấy toàn bộ trách nhiệm đang đặt lên mình. → `P6 +0`

### **P6.3**

Nếu một sai sót của bạn có thể gây hậu quả lớn cho công việc:

**A.** Tôi tập trung kiểm tra, xử lý và giảm thiểu hậu quả. → `P6 +2`  
 **B.** Tôi báo cho người liên quan và cùng tìm cách xử lý. → `P6 +1`  
 **C.** Tôi dễ bị áp lực bởi hậu quả và khó hành động ngay. → `P6 +0`

**4\. TỔNG HỢP KẾT QUẢ**

Mỗi dimension:

> **3 câu × 2 điểm \= tối đa 6 điểm**

| Dimension | Nội dung | Số câu | Điểm tối đa |
| ----- | ----- | ----- | ----- |
| **P1 — Time & Pace** | Thời gian & tốc độ | 3 | 6 |
| **P2 — Workload** | Khối lượng công việc | 3 | 6 |
| **P3 — Cognitive** | Tư duy & quyết định | 3 | 6 |
| **P4 — Emotional** | Cảm xúc | 3 | 6 |
| **P5 — Interpersonal** | Tương tác & xung đột | 3 | 6 |
| **P6 — Responsibility** | Trách nhiệm & hậu quả | 3 | 6 |
| **P — Pressure** | **Tổng** | **18** | **36** |

### **Công thức**

> **P1 \= (điểm đạt được / 6\) × 100**

Tương tự cho P2 → P6.

> **Overall Pressure Response \= (tổng điểm / 36\) × 100**

# **5\. OUTPUT CHO NGƯỜI CHƠI**

Ví dụ:

### **PRESSURE PROFILE**

> **Overall Pressure Response — 78%**

| Dimension | Score |
| ----- | ----- |
| Time & Pace | **83%** |
| Workload | **67%** |
| Cognitive | **83%** |
| Emotional | **67%** |
| Interpersonal | **83%** |
| Responsibility | **83%** |

### **AI diễn giải**

> **Pressure Pattern: High Cognitive & Responsibility Response**

> Bạn có xu hướng duy trì hiệu quả khá tốt khi phải đưa ra quyết định, xử lý vấn đề và chịu trách nhiệm về kết quả. Tuy nhiên, hiệu suất của bạn có thể bị ảnh hưởng nhiều hơn khi phải duy trì khối lượng công việc cao trong thời gian dài.

