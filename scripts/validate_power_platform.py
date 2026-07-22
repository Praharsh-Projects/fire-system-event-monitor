#!/usr/bin/env python3
"""Validate repository Power Platform artifacts against pinned schemas."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from urllib.request import Request, urlopen

import json5
import jsonschema


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
POWER_PLATFORM_CONNECTORS_COMMIT = "674654de54c58b80a0f577153236f6fea92edb9c"
RAW_SCHEMA_ROOT = (
    "https://raw.githubusercontent.com/microsoft/PowerPlatformConnectors/"
    f"{POWER_PLATFORM_CONNECTORS_COMMIT}/schemas"
)


def load_json(path: Path) -> object:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def load_remote_json(url: str) -> object:
    request = Request(url, headers={"User-Agent": "fire-system-event-monitor-validator"})
    with urlopen(request, timeout=30) as response:
        # The upstream Microsoft schemas are JSONC-style documents and currently
        # contain comments and trailing commas, so parse them with JSON5.
        return json5.loads(response.read().decode("utf-8"))


def validate(document_path: Path, schema: object, label: str) -> None:
    document = load_json(document_path)
    validator_class = jsonschema.validators.validator_for(schema)
    validator_class.check_schema(schema)
    validator = validator_class(schema, format_checker=validator_class.FORMAT_CHECKER)
    errors = sorted(
        validator.iter_errors(document),
        key=lambda error: "/".join(str(part) for part in error.absolute_path),
    )
    if errors:
        for error in errors:
            location = "/".join(str(part) for part in error.absolute_path) or "<root>"
            print(f"{label}: {location}: {error.message}", file=sys.stderr)
        raise SystemExit(1)
    print(f"validated {label}: {document_path.relative_to(REPOSITORY_ROOT)}")


def main() -> None:
    connector_root = REPOSITORY_ROOT / "power-platform" / "connector"
    swagger_schema = load_remote_json(f"{RAW_SCHEMA_ROOT}/apiDefinition.swagger.schema.json")
    properties_schema = load_remote_json(f"{RAW_SCHEMA_ROOT}/paconn-apiProperties.schema.json")

    validate(connector_root / "apiDefinition.swagger.json", swagger_schema, "connector definition")
    validate(connector_root / "apiProperties.json", properties_schema, "connector properties")

    flow_root = REPOSITORY_ROOT / "power-platform" / "power-automate"
    validate(
        flow_root / "critical-incident-triage.flow.json",
        load_json(flow_root / "flow-blueprint.schema.json"),
        "Power Automate flow blueprint",
    )


if __name__ == "__main__":
    main()
