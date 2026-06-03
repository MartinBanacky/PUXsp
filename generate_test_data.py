#!/usr/bin/env python3
"""
Generate nested test data for the PUXsp directory analyzer.

Examples:
    python generate_test_data.py C:\dev\PUXspTEST --depth 8
    python generate_test_data.py C:\dev\PUXspTEST --depth 5 --dirs-per-level 2 --files-per-dir 6 --file-size-kb 256
"""

from __future__ import annotations

import argparse
from pathlib import Path


LOREM_WORDS = [
    "lorem",
    "ipsum",
    "dolor",
    "sit",
    "amet",
    "consectetur",
    "adipiscing",
    "elit",
    "sed",
    "do",
    "eiusmod",
    "tempor",
    "incididunt",
    "ut",
    "labore",
    "et",
    "dolore",
    "magna",
    "aliqua",
    "enim",
]

FILE_EXTENSIONS = [".txt", ".log", ".json", ".csv", ".md", ".bin"]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a nested directory structure with lorem-ipsum file and directory names."
    )
    parser.add_argument(
        "target_path",
        help="Path where the generated structure will be created.",
    )
    parser.add_argument(
        "--depth",
        type=int,
        required=True,
        help="Directory depth to generate. Depth 1 means only the target directory itself receives files.",
    )
    parser.add_argument(
        "--dirs-per-level",
        type=int,
        default=1,
        help="How many child directories to create at each level. Default: 1.",
    )
    parser.add_argument(
        "--files-per-dir",
        type=int,
        default=5,
        help="How many files to create in each directory. Default: 5.",
    )
    parser.add_argument(
        "--file-size-kb",
        type=int,
        default=64,
        help="Approximate size of each generated file in KB. Default: 64.",
    )
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Delete existing generated content inside the target directory before generating new data.",
    )
    return parser.parse_args()


def validate_args(args: argparse.Namespace) -> None:
    if args.depth < 1:
        raise ValueError("--depth must be at least 1.")
    if args.dirs_per_level < 1:
        raise ValueError("--dirs-per-level must be at least 1.")
    if args.files_per_dir < 0:
        raise ValueError("--files-per-dir cannot be negative.")
    if args.file_size_kb < 1:
        raise ValueError("--file-size-kb must be at least 1.")


def name_from_index(index: int, prefix: str) -> str:
    first = LOREM_WORDS[index % len(LOREM_WORDS)]
    second = LOREM_WORDS[(index + 7) % len(LOREM_WORDS)]
    third = LOREM_WORDS[(index + 13) % len(LOREM_WORDS)]
    return f"{prefix}_{first}_{second}_{third}_{index:03d}"


def build_file_content(relative_path: str, file_index: int, target_size_bytes: int) -> str:
    header = (
        f"Generated test file for PUXsp analyzer\n"
        f"Relative path: {relative_path}\n"
        f"File index: {file_index}\n"
        f"Lorem marker: {name_from_index(file_index, 'content')}\n\n"
    )
    filler_sentence = (
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\n"
    )
    content = header
    while len(content.encode("utf-8")) < target_size_bytes:
        content += filler_sentence
    return content


def clean_directory(target_path: Path) -> None:
    if not target_path.exists():
        return

    for path in sorted(target_path.rglob("*"), reverse=True):
        if path.is_file():
            path.unlink()
        elif path.is_dir():
            path.rmdir()


def generate_directory_tree(
    current_path: Path,
    root_path: Path,
    remaining_depth: int,
    dirs_per_level: int,
    files_per_dir: int,
    file_size_bytes: int,
    counters: dict[str, int],
) -> None:
    current_path.mkdir(parents=True, exist_ok=True)

    for _ in range(files_per_dir):
        file_index = counters["file"]
        extension = FILE_EXTENSIONS[file_index % len(FILE_EXTENSIONS)]
        file_name = f"{name_from_index(file_index, 'file')}{extension}"
        file_path = current_path / file_name
        relative_path = file_path.relative_to(root_path).as_posix()
        file_content = build_file_content(relative_path, file_index, file_size_bytes)
        file_path.write_text(file_content, encoding="utf-8")
        counters["file"] += 1

    if remaining_depth <= 1:
        return

    for _ in range(dirs_per_level):
        directory_index = counters["directory"]
        directory_name = name_from_index(directory_index, "dir")
        child_path = current_path / directory_name
        counters["directory"] += 1
        generate_directory_tree(
            child_path,
            root_path,
            remaining_depth - 1,
            dirs_per_level,
            files_per_dir,
            file_size_bytes,
            counters,
        )


def main() -> None:
    args = parse_args()
    validate_args(args)

    target_path = Path(args.target_path).expanduser().resolve()
    target_path.mkdir(parents=True, exist_ok=True)

    if args.clean:
        clean_directory(target_path)

    counters = {"directory": 0, "file": 0}
    generate_directory_tree(
        current_path=target_path,
        root_path=target_path,
        remaining_depth=args.depth,
        dirs_per_level=args.dirs_per_level,
        files_per_dir=args.files_per_dir,
        file_size_bytes=args.file_size_kb * 1024,
        counters=counters,
    )

    print(f"Generated data in: {target_path}")
    print(f"Depth: {args.depth}")
    print(f"Directories created: {counters['directory']}")
    print(f"Files created: {counters['file']}")
    print(f"Approximate size per file: {args.file_size_kb} KB")


if __name__ == "__main__":
    main()
