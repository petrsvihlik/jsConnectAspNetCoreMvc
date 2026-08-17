#!/usr/bin/env python3
"""Sanity-check the cobertura report before handing it to Codecov.

An upload only fails when the file is missing or malformed, so a report that
parses but covers nothing would sail through and quietly zero out the Codecov
trend. Fail here instead, and print the totals so every run shows its numbers.
"""

import sys
import xml.etree.ElementTree as ET

path = sys.argv[1] if len(sys.argv) > 1 else "coverage/coverage.cobertura.xml"

try:
    root = ET.parse(path).getroot()
except (OSError, ET.ParseError) as err:
    sys.exit(f"could not read coverage report {path}: {err}")

packages = [p.get("name") for p in root.iter("package")]
lines = [line for line in root.iter("line")]
covered = sum(1 for line in lines if int(line.get("hits", "0")) > 0)

print(f"coverage: {covered}/{len(lines)} lines covered")
for name in packages:
    print(f"  package: {name}")

if not packages:
    sys.exit(f"{path} contains no packages — check the ModulePaths filter")
if covered == 0:
    sys.exit(f"{path} reports zero covered lines")
