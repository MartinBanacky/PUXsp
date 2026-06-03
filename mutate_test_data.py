#!/usr/bin/env python3
"""
Mutate an existing test directory for the PUXsp analyzer.

Examples:
    python mutate_test_data.py C:\\dev\\PUXspTEST
    python mutate_test_data.py C:\\dev\\PUXspTEST --modify-files 5 --delete-files 3 --add-files 4 --delete-dirs 1 --add-dirs 2
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
        description="Delete, modify, and add entries inside an existing generated test directory."
    )
    parser.add_argument("target_path", help="Path to an existing generated directory tree.")
    parser.add_argument("--modify-files", type=int, default=3, help="How many existing files to modify. Default: 3.")
    parser.add_argument("--delete-files", type=int, default=2, help="How many existing files to delete. Default: 2.")
    parser.add_argument("--add-files", type=int, default=3, help="How many new files to add. Default: 3.")
    parser.add_argument("--delete-dirs", type=int, default=1, help="How many existing subdirectories to delete. Default: 1.")
    parser.add_argument("--add-dirs", type=int, default=1, help="How many new subdirectories to add. Default: 1.")
    return parser.parse_args()


def validate_args(args: argparse.Namespace) -> None:
    for field_name in ["modify_files", "delete_files", "add_files", "delete_dirs", "add_dirs"]:
        if getattr(args, field_name) < 0:
            raise ValueError(f"--{field_name.replace('_', '-')} cannot be negative.")


def name_from_index(index: int, prefix: str) -> str:
    first = LOREM_WORDS[index % len(LOREM_WORDS)]
    second = LOREM_WORDS[(index + 5) % len(LOREM_WORDS)]
    third = LOREM_WORDS[(index + 11) % len(LOREM_WORDS)]
    return f"{prefix}_{first}_{second}_{third}_{index:03d}"


def append_modification_marker(file_path: Path, mutation_index: int) -> None:
    marker = (
        f"\nMutation marker {mutation_index}\n"
        f"Lorem payload: {name_from_index(mutation_index, 'mutation')}\n"
    )
    with file_path.open("a", encoding="utf-8", errors="ignore") as handle:
        handle.write(marker)


def delete_files(files: list[Path], count: int) -> int:
    deleted = 0
    for file_path in files[:count]:
        if file_path.exists():
            file_path.unlink()
            deleted += 1
    return deleted


def modify_files(files: list[Path], count: int) -> int:
    modified = 0
    for index, file_path in enumerate(files[:count], start=1):
        if file_path.exists():
            append_modification_marker(file_path, index)
            modified += 1
    return modified


def delete_directories(directories: list[Path], count: int) -> int:
    deleted = 0
    for directory in directories[:count]:
        if not directory.exists():
            continue

        for path in sorted(directory.rglob("*"), reverse=True):
            if path.is_file():
                path.unlink()
            elif path.is_dir():
                path.rmdir()

        directory.rmdir()
        deleted += 1

    return deleted


def add_directories(root_path: Path, count: int, existing_dir_count: int) -> int:
    created = 0
    for offset in range(count):
        directory_name = name_from_index(existing_dir_count + offset, "added_dir")
        new_directory = root_path / directory_name
        if new_directory.exists():
            continue

        new_directory.mkdir(parents=True, exist_ok=True)
        created += 1

    return created


def add_files(root_path: Path, directories: list[Path], count: int, existing_file_count: int) -> int:
    if not directories:
        directories = [root_path]

    created = 0
    for offset in range(count):
        file_index = existing_file_count + offset
        extension = FILE_EXTENSIONS[file_index % len(FILE_EXTENSIONS)]
        file_name = f"{name_from_index(file_index, 'added_file')}{extension}"
        target_directory = directories[offset % len(directories)]
        file_path = target_directory / file_name
        if file_path.exists():
            continue

        content = (
            f"Added file for mutation scenario\n"
            f"File index: {file_index}\n"
            f"Lorem marker: {name_from_index(file_index, 'added_content')}\n"
        )
        file_path.write_text(content, encoding="utf-8")
        created += 1

    return created


def main() -> None:
    args = parse_args()
    validate_args(args)

    root_path = Path(args.target_path).expanduser().resolve()
    if not root_path.exists() or not root_path.is_dir():
        raise ValueError("The target path must exist and be a directory.")

    existing_files = sorted([path for path in root_path.rglob("*") if path.is_file()])
    existing_directories = sorted([path for path in root_path.rglob("*") if path.is_dir()], reverse=True)

    modified_count = modify_files(existing_files, args.modify_files)
    deleted_file_count = delete_files(existing_files[modified_count:], args.delete_files)
    deleted_dir_count = delete_directories(existing_directories, args.delete_dirs)

    refreshed_directories = sorted([path for path in root_path.rglob("*") if path.is_dir()])
    added_dir_count = add_directories(root_path, args.add_dirs, len(refreshed_directories))
    refreshed_directories = sorted([path for path in root_path.rglob("*") if path.is_dir()])
    added_file_count = add_files(root_path, refreshed_directories, args.add_files, len(existing_files))

    print(f"Target path: {root_path}")
    print(f"Modified files: {modified_count}")
    print(f"Deleted files: {deleted_file_count}")
    print(f"Deleted directories: {deleted_dir_count}")
    print(f"Added directories: {added_dir_count}")
    print(f"Added files: {added_file_count}")


if __name__ == "__main__":
    main()
