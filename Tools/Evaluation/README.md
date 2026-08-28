# Duplicate Detection Evaluation

A standalone console tool that measures how well semantic similarity separates
duplicate, related, and unrelated GitHub issues.

## Running it

```bash
dotnet run --project Tools/Evaluation/IssueSense.Evaluation.csproj
```

Optional flags:

```bash
dotnet run --project Tools/Evaluation/IssueSense.Evaluation.csproj -- \
  --dataset path/to/other-dataset.json \
  --thresholds 0.5,0.6,0.7,0.8 \
  --output report.txt
```

The first run downloads the `all-MiniLM-L6-v2` ONNX model into
`%LOCALAPPDATA%/IssueSense/embedding-models` (same cache used by the API).
No database or GitHub credentials are required — the tool only exercises
`IEmbeddingService`.

## Dataset format

`Datasets/duplicate-detection-eval.json` is a JSON array of cases:

```json
{
  "id": "dup-01-crash-startup",
  "newIssue": { "title": "...", "body": "..." },
  "existingIssue": { "title": "...", "body": "..." },
  "expectedRelationship": "Duplicate"
}
```

`expectedRelationship` is one of `Duplicate`, `Related`, or `Unrelated`.
`Related` cases (same general area, genuinely different problem) are the
important negative examples — they're what separates real duplicate
detection from a bag-of-words similarity check.

## What the report measures

`Duplicate` is treated as the positive class; `Related` and `Unrelated` are
both negative, so a `Related` pair scoring above the threshold counts as a
false positive. The report sweeps a set of thresholds and reports precision,
recall, F1, false-positive rate, and false-negative rate at each, then
evaluates the thresholds currently configured in `DuplicateDetectionOptions`.

## Methodology: this dataset is held out, not a tuning set

**Do not adjust `DuplicateDetectionOptions` thresholds, the embedding model,
or any similarity logic in order to make this specific dataset score
better.** Doing so would just be fitting to 30 known examples, not improving
real-world duplicate detection, and the reported metrics would stop meaning
anything.

If detection quality needs improving:

1. Change the approach based on reasoning about the underlying problem (a
   different embedding model, a different similarity metric, chunking
   strategy, etc.) — not based on which threshold happens to maximize F1
   here.
2. Re-run the evaluation to see the effect, the same way you'd run it before
   any other change.
3. Add new cases (ideally from real false positives/negatives seen in
   production) to `Datasets/` to keep the dataset representative — but treat
   it as a check on the system, not an input to it.

Keep this dataset separate from anything used during development (ad hoc
manual testing, exploratory notebooks, etc.) so it stays a meaningful,
independent signal of accuracy over time.
