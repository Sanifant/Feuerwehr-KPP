"""Combine CI TRX and JUnit artifacts into a GitHub Actions job summary."""

import argparse
from collections import Counter
from dataclasses import dataclass
import html
import json
import os
from pathlib import Path
import xml.etree.ElementTree as ET


SUITES = (
    ("Backend unit tests", "backend-build-and-test", "backend-tests.trx"),
    ("App unit tests", "backend-build-and-test", "app-tests.trx"),
    ("Frontend tests", "frontend-build-and-test", "vitest-junit.xml"),
    ("API integration tests", "integration-tests", "integration-tests.trx"),
)
# Leave room below GitHub's 1 MiB per-step summary limit.
DETAIL_BUDGET = 800_000


@dataclass
class TestCase:
    name: str
    outcome: str
    seconds: float
    message: str = ""


def duration(value: str) -> float:
    """Accept TRX hours:minutes:seconds and JUnit seconds."""
    return sum(float(part) * 60**index for index, part in enumerate(reversed(value.split(":"))))


def parse_report(path: Path) -> list[TestCase]:
    root = ET.parse(path).getroot()
    cases = []
    if path.suffix == ".trx":
        for result in root.findall(".//{*}UnitTestResult"):
            outcome = {"Passed": "Passed", "NotExecuted": "Skipped"}.get(
                result.get("outcome"), "Failed"
            )
            message = result.find("./{*}Output/{*}ErrorInfo/{*}Message")
            cases.append(TestCase(
                result.get("testName", "Unnamed test"), outcome,
                duration(result.get("duration", "0")),
                "" if message is None else "".join(message.itertext()),
            ))
    else:
        for result in root.findall(".//testcase"):
            failure = result.find("failure")
            if failure is None:
                failure = result.find("error")
            outcome = "Failed" if failure is not None else (
                "Skipped" if result.find("skipped") is not None else "Passed"
            )
            cases.append(TestCase(
                f"{result.get('classname', '')} {result.get('name', 'Unnamed test')}".strip(),
                outcome, duration(result.get("time", "0")),
                "" if failure is None else failure.get("message", "") + "\n" + "".join(failure.itertext()),
            ))
    if not cases:
        raise ValueError("Report contains no test results; inspect the job logs for runner errors.")
    return cases


def cell(value: str) -> str:
    # Test names can contain HTML, Markdown, pipes and line breaks.
    value = html.escape(" ".join(value.split()))
    for character in "|`*_[\\":
        value = value.replace(character, f"&#{ord(character)};")
    return value


def summarize(results: Path, needs: dict) -> tuple[str, bool]:
    lines = [
        "## Test results", "",
        "| Suite | CI job | Total | Passed | Failed | Skipped | Test time |",
        "| --- | --- | ---: | ---: | ---: | ---: | ---: |",
    ]
    totals = Counter()
    seconds = 0.0
    details = []
    notes = []
    invalid = False
    for label, job, filename in SUITES:
        status = needs.get(job, {}).get("result", "unknown")
        paths = sorted(results.rglob(filename))
        cases = []
        for path in paths:
            try:
                cases.extend(parse_report(path))
            except (ET.ParseError, OSError, ValueError) as error:
                notes.append(f"**{label}:** Could not read {cell(path.name)}: {cell(str(error))}")
                invalid = True
        if not cases:
            lines.append(f"| {label} | {cell(status)} | — | — | — | — | — |")
            if not paths:
                notes.append(f"**{label}:** No report available (CI job: {cell(status)}).")
                invalid |= status == "success"
            continue
        counts = Counter(case.outcome for case in cases)
        elapsed = sum(case.seconds for case in cases)
        totals.update(counts)
        seconds += elapsed
        lines.append(
            f"| {label} | {cell(status)} | {len(cases)} | {counts['Passed']} | "
            f"{counts['Failed']} | {counts['Skipped']} | {elapsed:.2f}s |"
        )
        details.append((label, cases))
    lines.extend([
        f"| **Total reported** | | **{sum(totals.values())}** | **{totals['Passed']}** | "
        f"**{totals['Failed']}** | **{totals['Skipped']}** | **{seconds:.2f}s** |",
        "",
        "CI job status includes build and artifact upload steps. Test time is the sum of individual test durations.",
        "Integration tests run on pull requests, releases, and pushes to main/master after the backend job succeeds.",
        "",
    ])
    lines.extend(note + "\n" for note in notes)
    budget = DETAIL_BUDGET
    truncated = False
    # Show failures before the full inventory so they remain visible in large runs.
    for label, cases in details:
        for case in cases:
            if case.outcome != "Failed":
                continue
            block = (
                f"<details><summary>{label}: {cell(case.name[:1000])}</summary>\n\n"
                f"<pre>{html.escape(case.message[:3000] or 'No failure message; see the report artifact.')}</pre>\n\n"
                "</details>\n"
            )
            size = len(block.encode("utf-8"))
            if size > budget:
                truncated = True
                break
            lines.append(block)
            budget -= size
    for label, cases in details:
        lines.extend([
            f"<details><summary>{label}: all {len(cases)} tests</summary>", "",
            "| Result | Test | Duration |", "| --- | --- | ---: |",
        ])
        for case in sorted(cases, key=lambda case: (case.outcome, case.name)):
            row = f"| {case.outcome} | {cell(case.name[:2000])} | {case.seconds:.3f}s |"
            size = len(row.encode("utf-8"))
            if size > budget:
                truncated = True
                break
            lines.append(row)
            budget -= size
        lines.extend(["", "</details>", ""])
    if truncated:
        lines.append("Some test details were omitted to fit the summary size limit. Download the report artifacts for the full results.")
    lines.append("Full TRX/JUnit reports are available in the workflow run's test-results artifacts.")
    return "\n".join(lines) + "\n", invalid


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("results", type=Path)
    parser.add_argument("--output", type=Path, default=os.environ.get("GITHUB_STEP_SUMMARY", "test-summary.md"))
    args = parser.parse_args()
    summary, invalid = summarize(args.results, json.loads(os.environ.get("CI_NEEDS", "{}")))
    with args.output.open("a", encoding="utf-8") as output:
        output.write(summary)
    return 1 if invalid else 0


if __name__ == "__main__":
    raise SystemExit(main())
