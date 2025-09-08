<div align="center">

![ditto](https://github.com/eastcitysoftware/ditto/blob/assets/ditto.png?raw=true)

[![build](https://github.com/eastcitysoftware/ditto/actions/workflows/build.yml/badge.svg)](https://github.com/eastcitysoftware/ditto/actions/workflows/build.yml)
![License](https://img.shields.io/github/license/eastcitysoftware/ditto)

Build websites with [Scriban](https://github.com/scriban/scriban) & [TOML](https://github.com/prozolic/CsToml).
</div>

---

Ditto is a static website generator that uses TOML for configuration and Scriban for templating/scripting. It provides a localized, data-driven approach to building websites.

---

## Usage

```shell
Description:
  ditto, static webite generator with hot reload

Usage:
  Ditto <input> [options]

Arguments:
  <input>  The absolute path to website directory containing website.toml

Options:
  -?, -h, --help         Show help and usage information
  --version              Show version information
  output, -o (REQUIRED)  The output directory for the generated files
```
