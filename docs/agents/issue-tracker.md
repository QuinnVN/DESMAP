# Issue tracker: Local Markdown

Issues and specs for this repository live as Markdown files in `.scratch/`.

## Conventions

- One feature per directory: `.scratch/<feature-slug>/`
- The specification is `.scratch/<feature-slug>/spec.md`.
- Implementation issues are separate files under `.scratch/<feature-slug>/issues/`.
- Issue filenames use `<NN>-<slug>.md`, numbered from `01`.
- Comments append under a `## Comments` heading.

## Publishing

When a skill says "publish to the issue tracker", create the requested Markdown file under `.scratch/<feature-slug>/`.

When a skill says "fetch the relevant ticket", read the referenced file. The user will normally provide its path or issue number.
