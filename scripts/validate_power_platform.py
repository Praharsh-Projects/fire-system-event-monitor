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


def require_repository_file(relative_path: str, label: str) -> None:
    candidate = (REPOSITORY_ROOT / relative_path).resolve()
    try:
        candidate.relative_to(REPOSITORY_ROOT.resolve())
    except ValueError as error:
        raise SystemExit(f"{label}: path escapes the repository: {relative_path}") from error

    if not candidate.is_file():
        raise SystemExit(f"{label}: required file is missing: {relative_path}")

    print(f"validated {label}: {relative_path}")


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

    dynamics_root = REPOSITORY_ROOT / "dynamics365"
    deployment_manifest_path = dynamics_root / "deployment-manifest.json"
    validate(
        deployment_manifest_path,
        load_json(dynamics_root / "deployment-manifest.schema.json"),
        "Dynamics 365 CE deployment manifest",
    )

    deployment_manifest = load_json(deployment_manifest_path)
    if not isinstance(deployment_manifest, dict):
        raise SystemExit("Dynamics 365 CE deployment manifest must be an object")

    plugin_assembly = deployment_manifest.get("pluginAssembly")
    if not isinstance(plugin_assembly, dict):
        raise SystemExit("Dynamics 365 CE plug-in assembly definition is missing")
    plugin_project = plugin_assembly.get("project")
    if not isinstance(plugin_project, str):
        raise SystemExit("Dynamics 365 CE plug-in project path is missing")
    require_repository_file(plugin_project, "Dynamics 365 CE plug-in project")
    require_repository_file(
        "dynamics365/plugins/FireSystemEventMonitor.Dataverse.Plugin/"
        "FireSystemEventMonitor.snk",
        "Dynamics 365 CE development signing key",
    )

    web_resources = deployment_manifest.get("webResources")
    if not isinstance(web_resources, list):
        raise SystemExit("Dynamics 365 CE web resource definitions are missing")
    for index, resource in enumerate(web_resources):
        if not isinstance(resource, dict) or not isinstance(resource.get("source"), str):
            raise SystemExit(f"Dynamics 365 CE web resource {index} has no source path")
        require_repository_file(
            resource["source"],
            f"Dynamics 365 CE web resource {index}",
        )


if __name__ == "__main__":
    main()
