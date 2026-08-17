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


def conditions(line):
    """Covered/total branches for a line, from condition-coverage="50% (1/2)"."""
    text = line.get("condition-coverage", "")
    if "(" not in text or "/" not in text:
        return 0, 0
    covered, _, total = text[text.index("(") + 1 : text.rindex(")")].partition("/")
    try:
        return int(covered), int(total)
    except ValueError:
        return 0, 0


def pct(covered, total):
    return f"{covered / total:.1%}" if total else "n/a"


def bar(covered, total, width=24):
    filled = round(width * covered / total) if total else 0
    return "█" * filled + "░" * (width - filled)


class Tally:
    def __init__(self):
        self.lines = self.hit = self.branches = self.branches_hit = 0

    def add(self, line):
        self.lines += 1
        if int(line.get("hits", "0")) > 0:
            self.hit += 1
        covered, total = conditions(line)
        self.branches += total
        self.branches_hit += covered


assemblies = {}
for package in root.iter("package"):
    tally = assemblies.setdefault(package.get("name") or "(unnamed)", Tally())
    for cls in package.iter("class"):
        for line in class_lines(cls):
            tally.add(line)

total_lines = sum(t.lines for t in assemblies.values())
total_hit = sum(t.hit for t in assemblies.values())
total_branches = sum(t.branches for t in assemblies.values())
total_branches_hit = sum(t.branches_hit for t in assemblies.values())

print(f"coverage: {pct(total_hit, total_lines)} ({total_hit}/{total_lines} lines)")
for name, tally in sorted(assemblies.items()):
    print(f"  {name}: {pct(tally.hit, tally.lines)} ({tally.hit}/{tally.lines} lines)")

summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
if summary_path and assemblies:
    branch_note = (
        f" · **{total_branches_hit} of {total_branches} branches** "
        f"({pct(total_branches_hit, total_branches)})"
        if total_branches
        else ""
    )
    out = [
        f"## Coverage — {pct(total_hit, total_lines)}",
        "",
        f"`{bar(total_hit, total_lines)}`",
        "",
        f"**{total_hit} of {total_lines} lines**{branch_note}",
        "",
        "| Assembly | Coverage | Lines | Branches |",
        "| --- | ---: | ---: | ---: |",
    ]
    for name, tally in sorted(assemblies.items()):
        branches = f"{tally.branches_hit}/{tally.branches}" if tally.branches else "—"
        out.append(
            f"| `{name}` | {pct(tally.hit, tally.lines)} "
            f"| {tally.hit}/{tally.lines} | {branches} |"
        )
    with open(summary_path, "a", encoding="utf-8") as summary:
        summary.write("\n".join(out) + "\n")

if not assemblies:
    sys.exit(f"{path} contains no assemblies — check the ModulePaths filter")
if total_hit == 0:
    sys.exit(f"{path} reports zero covered lines")
