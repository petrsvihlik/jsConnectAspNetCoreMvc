#!/usr/bin/env python3
"""Summarise the cobertura report and fail the run if it covers nothing.

codecov-action only fails when the report is missing or malformed, so a report
that parses but covers nothing uploads happily and quietly zeroes out the
Codecov trend. That is a live risk whenever the ModulePaths filter in
CodeCoverage.config stops matching the assemblies it is meant to match.

Writes a per-assembly table to the workflow run summary when GITHUB_STEP_SUMMARY
is set, and the same numbers to stdout for the step log.
"""

import os
import sys
import xml.etree.ElementTree as ET

path = sys.argv[1] if len(sys.argv) > 1 else "coverage/coverage.cobertura.xml"

try:
    root = ET.parse(path).getroot()
except (OSError, ET.ParseError) as err:
    sys.exit(f"could not read coverage report {path}: {err}")


def class_lines(cls):
    """Lines belonging to a class, without the copies nested under methods.

    Cobertura repeats every line inside <method><lines> as well as in the
    class-level <lines>, so iterating all descendants double-counts.
    """
    lines = cls.find("lines")
    return lines.findall("line") if lines is not None else []


def tally(lines):
    return sum(1 for line in lines if int(line.get("hits", "0")) > 0), len(lines)


def rate(covered, total):
    return f"{covered / total:.1%}" if total else "n/a"


packages = []
for package in root.iter("package"):
    lines = [line for cls in package.iter("class") for line in class_lines(cls)]
    covered, total = tally(lines)
    packages.append((package.get("name") or "(unnamed)", covered, total))

total_covered = sum(covered for _, covered, _ in packages)
total_lines = sum(total for _, _, total in packages)

print(f"coverage: {rate(total_covered, total_lines)} ({total_covered}/{total_lines} lines)")
for name, covered, total in packages:
    print(f"  {name}: {rate(covered, total)} ({covered}/{total} lines)")

summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
if summary_path:
    rows = [
        "## Coverage",
        "",
        f"**{rate(total_covered, total_lines)}** — {total_covered} of {total_lines} lines covered",
        "",
        "| Assembly | Coverage | Lines |",
        "| --- | --- | --- |",
    ]
    rows += [
        f"| {name} | {rate(covered, total)} | {covered}/{total} |"
        for name, covered, total in sorted(packages)
    ]
    with open(summary_path, "a", encoding="utf-8") as summary:
        summary.write("\n".join(rows) + "\n")

if not packages:
    sys.exit(f"{path} contains no assemblies — check the ModulePaths filter")
if total_covered == 0:
    sys.exit(f"{path} reports zero covered lines")
