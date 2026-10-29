#!/usr/bin/env python3
"""Publish the repository's existing GitBook pages after a GitHub release."""

from __future__ import annotations

import json
import os
import posixpath
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


API_BASE_URL = os.environ.get("GITBOOK_API_BASE_URL", "https://api.gitbook.com/v1")
DEFAULT_SPACE_ID = "rqOPZTUPW3ossaksi6Ei"
DOC_PAGES = {
    "README.md": "feuerwehr-kpp-dokumentation",
    "getting-started.md": "orientierung/projektuberblick-und-entwicklungsstart",
    "web-application.md": "orientierung/webanwendung",
    "screenshots.md": "orientierung/avalonia-client-und-screenshots",
    "AUTHENTICATION.md": "funktionen/authentifizierung",
    "AUTH_QUICK_REFERENCE.md": "funktionen/kurzubersicht-authentifizierung",
    "IMPLEMENTATION_SUMMARY.md": "funktionen/implementierungsstand",
    "incident-modules.md": "funktionen/lagekarte-und-einsatztagebuch",
    "architecture.md": "entwicklung-und-architektur/systemarchitektur",
    "api/openapi.md": "entwicklung-und-architektur/openapi-fur-gitbook-erzeugen",
    "api/README.md": "entwicklung-und-architektur/api-ubersicht",
    "adr/0001-use-monorepo.md": "architekturentscheidungen/adr-0001-use-a-monorepository",
    "adr/0002-use-avalonia-for-cross-platform-clients.md": (
        "architekturentscheidungen/"
        "adr-0002-use-avalonia-ui-for-cross-platform-user-interfaces"
    ),
}

IMAGE_PATTERN = re.compile(r"!\[([^\]]*)\]\(([^)]+)\)")
LINK_PATTERN = re.compile(r"(?<!!)\[([^\]]+)\]\(([^)]+)\)")
FENCE_PATTERN = re.compile(r"^\s*(`{3,}|~{3,})")


def api_request(
    method: str, path: str, token: str, body: dict | None = None
) -> dict:
    url = f"{API_BASE_URL.rstrip('/')}{path}"
    data = json.dumps(body, ensure_ascii=False).encode("utf-8") if body else None
    headers = {
        "Accept": "application/json",
        "Authorization": f"Bearer {token}",
    }
    if data is not None:
        headers["Content-Type"] = "application/json"

    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            response_body = response.read()
    except urllib.error.HTTPError as error:
        details = error.read().decode("utf-8", errors="replace")
        raise RuntimeError(
            f"GitBook API returned HTTP {error.code} for {method} {path}: {details}"
        ) from error
    except urllib.error.URLError as error:
        raise RuntimeError(
            f"Could not reach the GitBook API for {method} {path}: {error.reason}"
        ) from error

    if not response_body:
        return {}
    try:
        result = json.loads(response_body)
    except json.JSONDecodeError as error:
        raise RuntimeError(
            f"GitBook API returned invalid JSON for {method} {path}."
        ) from error
    if not isinstance(result, dict):
        raise RuntimeError(f"GitBook API returned an unexpected response for {path}.")
    return result


def flatten_pages(pages: list[dict]) -> dict[str, str]:
    result: dict[str, str] = {}
    for page in pages:
        page_path = page.get("path")
        page_id = page.get("id")
        if isinstance(page_path, str) and isinstance(page_id, str):
            result[page_path] = page_id
        children = page.get("pages", [])
        if isinstance(children, list):
            result.update(flatten_pages(children))
    return result


def rewrite_target(
    target: str, source_path: str, repo: str, commit_sha: str
) -> str:
    parsed = urllib.parse.urlsplit(target)
    if parsed.scheme or target.startswith(("/", "#")):
        return target

    relative_path = posixpath.normpath(
        posixpath.join(posixpath.dirname(source_path), parsed.path)
    )
    page_path = DOC_PAGES.get(relative_path)
    if page_path:
        return f"/pages/{page_path}" + (
            f"#{parsed.fragment}" if parsed.fragment else ""
        )

    if relative_path.startswith("screenshots/"):
        encoded_sha = urllib.parse.quote(commit_sha, safe="")
        encoded_path = urllib.parse.quote(relative_path, safe="/")
        return (
            f"https://raw.githubusercontent.com/{repo}/{encoded_sha}/docs/"
            f"{encoded_path}"
        )

    if relative_path.lower().endswith(".md"):
        raise ValueError(
            f"Unmapped local Markdown link in docs/{source_path}: {target}"
        )
    return target


def rewrite_markdown(
    markdown: str, source_path: str, repo: str, commit_sha: str
) -> str:
    rewritten: list[str] = []
    fence_char: str | None = None
    fence_length = 0

    for line in markdown.splitlines():
        match = FENCE_PATTERN.match(line)
        if match:
            marker = match.group(1)
            if fence_char is None:
                fence_char = marker[0]
                fence_length = len(marker)
            elif marker[0] == fence_char and len(marker) >= fence_length:
                fence_char = None
                fence_length = 0
            rewritten.append(line)
            continue

        if fence_char is not None:
            rewritten.append(line)
            continue

        line = IMAGE_PATTERN.sub(
            lambda match: (
                f"![{match.group(1)}]("
                f"{rewrite_target(match.group(2), source_path, repo, commit_sha)})"
            ),
            line,
        )
        line = LINK_PATTERN.sub(
            lambda match: (
                f"[{match.group(1)}]("
                f"{rewrite_target(match.group(2), source_path, repo, commit_sha)})"
            ),
            line,
        )
        rewritten.append(line)

    if fence_char is not None:
        raise ValueError(f"Unclosed Markdown code fence in docs/{source_path}.")
    return "\n".join(rewritten) + "\n"


def build_changes(repo_root: Path, repo: str, commit_sha: str) -> list[dict]:
    changes = []
    for source_path, _ in DOC_PAGES.items():
        source_file = repo_root / "docs" / Path(source_path)
        if not source_file.is_file():
            raise FileNotFoundError(f"Documentation source is missing: {source_file}")
        markdown = source_file.read_text(encoding="utf-8")
        markdown = rewrite_markdown(markdown, source_path, repo, commit_sha)
        changes.append(
            {
                "operation": "update_page",
                "pagePath": DOC_PAGES[source_path],
                "document": {"markdown": markdown},
            }
        )
    return changes


def publish(repo_root: Path, dry_run: bool) -> None:
    repo = os.environ.get("GITHUB_REPOSITORY", "Sanifant/Feuerwehr-KPP")
    commit_sha = os.environ.get("GITHUB_SHA", "local")
    release_tag = os.environ.get("GITHUB_REF_NAME", "local")
    changes = build_changes(repo_root, repo, commit_sha)

    if dry_run:
        print(
            f"Validated {len(changes)} GitBook page updates for "
            f"{repo}@{commit_sha} (release {release_tag})."
        )
        return

    token = os.environ.get("GITBOOK_TOKEN")
    if not token:
        raise RuntimeError(
            "GITBOOK_TOKEN is required. Add a GitBook personal access token "
            "with edit access as a GitHub Actions secret."
        )

    space_id = os.environ.get("GITBOOK_SPACE_ID", DEFAULT_SPACE_ID)
    pages_response = api_request(
        "GET",
        f"/spaces/{urllib.parse.quote(space_id, safe='')}/content/pages",
        token,
    )
    pages = pages_response.get("pages")
    if not isinstance(pages, list):
        raise RuntimeError("GitBook page listing did not contain a pages array.")
    page_ids = flatten_pages(pages)

    resolved_changes = []
    for change in changes:
        page_id = page_ids.get(change["pagePath"])
        if not page_id:
            raise RuntimeError(
                f"GitBook page path is missing from space {space_id}: "
                f"{change['pagePath']}"
            )
        resolved_changes.append(
            {
                "operation": "update_page",
                "page": page_id,
                "document": change["document"],
            }
        )

    change_request = api_request(
        "POST",
        f"/spaces/{urllib.parse.quote(space_id, safe='')}/change-requests",
        token,
        {"subject": f"Publish documentation for release {release_tag}"},
    )
    change_request_id = change_request.get("id")
    if not isinstance(change_request_id, str):
        raise RuntimeError("GitBook did not return a change request ID.")

    content_path = (
        f"/spaces/{urllib.parse.quote(space_id, safe='')}/change-requests/"
        f"{urllib.parse.quote(change_request_id, safe='')}/content?compat=false"
    )
    try:
        api_request("POST", content_path, token, {"changes": resolved_changes})
    except Exception as error:
        raise RuntimeError(
            f"Could not update GitBook change request {change_request_id}; "
            "the draft was left open for inspection."
        ) from error

    merge_path = (
        f"/spaces/{urllib.parse.quote(space_id, safe='')}/change-requests/"
        f"{urllib.parse.quote(change_request_id, safe='')}/merge"
    )
    api_request("POST", merge_path, token)
    print(
        f"Published {len(resolved_changes)} documentation pages for release "
        f"{release_tag} to GitBook."
    )


def main() -> int:
    dry_run = "--dry-run" in sys.argv[1:]
    repo_root = Path(__file__).resolve().parent.parent
    try:
        publish(repo_root, dry_run)
    except (FileNotFoundError, RuntimeError, ValueError) as error:
        print(f"GitBook publication failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
