#!/usr/bin/env python3
import json
import sys

if len(sys.argv) != 2:
    raise SystemExit("usage: verify_production_dependencies.py PACKAGE_LIST_JSON")

with open(sys.argv[1], encoding="utf-8") as stream:
    document = json.load(stream)

projects = document.get("projects")
if not isinstance(projects, list) or not projects:
    raise SystemExit("production package listing contains no projects")

packages = []
for project in projects:
    frameworks = project.get("frameworks")
    if not isinstance(frameworks, list) or not frameworks:
        raise SystemExit("production package listing contains no target frameworks")
    for framework in frameworks:
        for key, value in framework.items():
            if key.endswith("Packages") and isinstance(value, list):
                for package in value:
                    name = package.get("id") if isinstance(package, dict) else str(package)
                    packages.append(name or "<unknown>")

if packages:
    raise SystemExit(
        "production project must not resolve external packages: "
        + ", ".join(sorted(set(packages))))

print("Production project resolves no external packages.")
