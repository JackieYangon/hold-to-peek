#!/usr/bin/env python3
"""Build an architecture-independent Ubuntu .deb using Python 3.8+ only."""
import gzip
import hashlib
import io
import re
import tarfile
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[1]


def tar_bytes(files):
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode="w", format=tarfile.GNU_FORMAT) as archive:
        directories = set()
        for name, data, mode in files:
            directories.update(str(parent) for parent in PurePosixPath(name).parents if str(parent) != ".")
        for name in sorted(directories, key=lambda value: (value.count("/"), value)):
            info = tarfile.TarInfo("./" + name)
            info.type = tarfile.DIRTYPE
            info.mode = 0o755
            info.uid = info.gid = info.mtime = 0
            info.uname = info.gname = "root"
            archive.addfile(info)
        for name, data, mode in sorted(files):
            info = tarfile.TarInfo(name)
            info.size = len(data)
            info.mode = mode
            info.uid = info.gid = info.mtime = 0
            info.uname = info.gname = "root"
            archive.addfile(info, io.BytesIO(data))
    return gzip.compress(output.getvalue(), mtime=0)


def ar_member(name, data):
    header = "{:<16}{:<12}{:<6}{:<6}{:<8}{:<10}`\n".format(name + "/", 0, 0, 0, "100644", len(data))
    return header.encode("ascii") + data + (b"\n" if len(data) % 2 else b"")


def build():
    version = (ROOT / "VERSION").read_text(encoding="utf-8").strip()
    if not re.fullmatch(r"\d+\.\d+\.\d+", version):
        raise ValueError("VERSION must contain a.b.c")
    source = ROOT / "src/HoldToPeek.Linux"
    files = []
    for path in (source / "hold_to_peek").glob("*.py"):
        files.append(("./usr/share/hold-to-peek/hold_to_peek/" + path.name, path.read_bytes(), 0o644))
    files.extend([
        ("./usr/bin/hold-to-peek", (source / "hold-to-peek").read_bytes().replace(b"\r\n", b"\n"), 0o755),
        ("./usr/share/hold-to-peek/VERSION", (version + "\n").encode(), 0o644),
        ("./usr/share/applications/hold-to-peek.desktop", (source / "hold-to-peek.desktop").read_bytes(), 0o644),
        ("./usr/share/doc/hold-to-peek/copyright", (ROOT / "LICENSE").read_bytes(), 0o644),
        ("./usr/share/doc/hold-to-peek/README.md", (ROOT / "docs/UBUNTU.md").read_bytes(), 0o644),
    ])
    control = ("Package: hold-to-peek\nVersion: " + version + "\nArchitecture: all\n"
               "Maintainer: JackieYangon <163224173+JackieYangon@users.noreply.github.com>\n"
               "Section: utils\nPriority: optional\n"
               "Depends: python3 (>= 3.8), python3-gi, python3-gi-cairo, python3-xlib, gir1.2-gtk-3.0, gir1.2-ayatanaappindicator3-0.1 | gir1.2-appindicator3-0.1\n"
               "Homepage: https://github.com/JackieYangon/hold-to-peek\n"
               "Description: Hold to peek at a reference screenshot\n"
               " An in-memory reference overlay for Ubuntu X11/Xorg desktops.\n"
               " Capture, hold to view, scroll to mark your place, and release to hide.\n"
               " Wayland sessions are not supported by this build.\n").encode("utf-8")
    checksums = "".join(hashlib.md5(data).hexdigest() + "  " + name[2:] + "\n" for name, data, mode in files)
    archive = b"!<arch>\n" + ar_member("debian-binary", b"2.0\n")
    archive += ar_member("control.tar.gz", tar_bytes([("./control", control, 0o644), ("./md5sums", checksums.encode(), 0o644)]))
    archive += ar_member("data.tar.gz", tar_bytes(files))
    destination = ROOT / "artifacts" / ("hold-to-peek_" + version + "_all.deb")
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists() and destination.read_bytes() != archive:
        raise FileExistsError("Refusing to overwrite a different versioned package: " + str(destination))
    destination.write_bytes(archive)
    print(str(destination))
    print("SHA256 " + hashlib.sha256(archive).hexdigest())
    return destination


if __name__ == "__main__":
    build()
