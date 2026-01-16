<div align="center">

![ditto](https://github.com/eastcitysoftware/ditto/blob/assets/ditto.png?raw=true)

[![build](https://github.com/eastcitysoftware/ditto/actions/workflows/build.yml/badge.svg)](https://github.com/eastcitysoftware/ditto/actions/workflows/build.yml)
![License](https://img.shields.io/github/license/eastcitysoftware/ditto)

Built-in _powerful_ scripting language, hot reload and dev server.
</div>

---

Ditto is a static website generator with powerful scripting capabilities, hot reload functionality, and a built-in development server. It leverages the [TOML](https://github.com/prozolic/CsToml) format for configuration and the [Scriban](https://github.com/scriban/scriban) templating engine for dynamic content generation, making it easy to create localized, _page-driven_ websites.

---

## Features

- **Powerful Scripting**: Utilize Scriban to create dynamic and customizable website content.
- **Hot Reload**: Automatically regenerate your website when changes are detected in the source files.
- **Built-in Dev Server**: Preview your website locally with a built-in web server that supports live reloading.
- **TOML Configuration**: Use the easy-to-read TOML format for configuring your website settings.

## Installation

??

Once the CLI is installed, you can use the `ditto` command in your terminal. Below is the output for the `ditto --help` command:

```shell
Description:
  ditto, static site generator with powerful scripting, hot reload, and a built-in dev server

Usage:
  ditto <input> [options]

Arguments:
  <input>  The absolute path to website directory containing website.toml

Options:
  output, -o <output> (REQUIRED)  The output directory for the generated files
  watch                           Enable watch, watches the input directory for changes and regenerates the website automatically
  serve                           Enable built-in web server
  port, -p <port>                 Set the port for the built-in web server [default: 8080]
  -?, -h, --help                  Show help and usage information
  --version                       Show version information
```

### Example Commands

Generate a website:

```shell
ditto /path/to/website -o /path/to/output
```

Generate a website and start the built-in web server with hot reload:

```shell
ditto /path/to/website -o /path/to/output --watch --serve
```

Generate a website and start the built-in web server on a specific port (e.g., 3000) with hot reload:

```shell
ditto /path/to/website -o /path/to/output --watch --serve --port 3000
```

Generate a website with watch enabled (no web server):

```shell
ditto /path/to/website -o /path/to/output --watch
```

## Usage

A typical solution will have a structure similar to the following:

```
my-website/
├── public/ <-- output directory
│   ├── static/ <-- should contain static files like images, css, js
├── src/
│   ├── _layouts/ <-- layouts for pages
│   │   ├── default.html <-- required
│   ├── _partials/ <-- reusable partials
│   │   ├── header.html
│   │   ├── footer.html
│   ├── index.html
│   ├── about.md <-- markdown and html can be mixed
│   ├── posts/ <-- subdirectory for posts, becomes a "page collection"
│   │   ├── 1999-12-12-welcome.md
│   │   ├── 1999-12-22-another-post.html
├── website.toml <-- configuration file
```

### Website Configuration

The `website.toml` file is the main configuration file for your Ditto website. It is expected to contain at least: `base_url`, `title`, and `description` fields.

These fields can be accessed within your templates using the `site` global (ex: `site.title`). Additional fields can be added as needed for your specific website requirements. These fields will also be accessible via the `site` global within it's `data` parameter (ex: `site.data.contact_phone`).

Below is an example configuration:

```toml
base_url = 'http://site.local'
title = 'Ditto Blog'
description = 'A blog about Ditto, a static site generator.'
# Optional, defaults to ' - '
title_separator = ' | '
# Custom field example
contact_phone = '+15555555555'
```

### Layouts

Layouts are stored in the `_layouts/` directory. Layouts define the overall structure of your pages and can include placeholders for dynamic content. You can define as many layouts as you need.

> A default layout named `default.html` is required!

Below is an example of a simple layout:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}}</title>
    <meta name="description" content="{{description}}">
    <link rel="stylesheet" href="/static/style.css">
</head>
<body>
    {{ content }}
</body>
</html>
```

Layouts have access to the [same variables as pages](#pages), but commonly include:
- `title`: The title of the current page.
- `content`: The rendered content of the current page.
- `description`: The description of the current page.

### Partials

Partials are reusable snippets of HTML that can be included in multiple layouts or pages. They are stored in the `_partials/` directory and included in a template using the `include` function. They are accessible in both layouts and pages.

For example, our layout above could be modified to include header and footer partials:

```html
<!-- _partials/header.html -->
<header>
    <h1>{{title}}</h1>
</header>

<!-- _partials/footer.html -->
<footer>
    <p>&copy; {{site.title}} {{ date.now.year }}</p>
</footer>

<!-- _layouts/default.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}}</title>
    <meta name="description" content="{{description}}">
    <link rel="stylesheet" href="/static/style.css">
</head>
<body>
    {{ include "header" }}
    {{ content }}
    {{ include "footer" }}
</body>
</html>
```

Partials also have access to the [same variables as pages](#pages), but they can also accept input parameters and assign local variables if needed.

Passing arguments is done positionally after the partial name:

```html
{{ include "partial_name" arg1 arg2 }}
```
These are accessible in the partial as `$1`, `$2`, etc.

```html
{{
    arg1 = $1
    arg2 = $2
}}
<p>Argument 1: {{ arg1 }}</p>
<p>Argument 2: {{ arg2 }}</p>
```

> Note how new script blocks are delimited with `{{` and `}}`. In the script block above we assign the passed arguments to local variables `arg1` and `arg2` for easier access.

If the partial is within a subdirectory of `_partials/`, use a forward slash to denote the path:

```html
{{ include "subdir/partial_name" arg1 arg2 }}
```

### Pages

Pages are the main content files of your website. They can be written in HTML or Markdown and are stored in the `src/` directory or its subdirectories. Markdown files should have a `.md` extension, while HTML files should have a `.html` extension.

> Pages can also _optionally_ include a front matter block at the top of the file for metadata.

Pages have access to several built-in variables:

| Variable      | Description                                      |
|---------------|--------------------------------------------------|
| `path`        | The file system path of the page.                |
| `slug`        | The URL-friendly slug of the page, derived from the filename.         |
| `url`         | The full URL of the page, combining the site's base URL and the page's slug. |
| `title`       | The title of the page, either from front matter or derived from the filename. |
| `page_title`  | The title of the page, plus the site title, `{page title}{site title separator}{site title}`. |
| `description` | The description of the page, from front matter if available. |
| `tags`        | The tags associated with the page, from front matter if available. |
| `published`   | The published date of the page, from front matter if available. |
| `data`        | A dictionary containing all front matter fields not mapped to other variables. |
| `site`        | A global variable containing site-wide configuration from `website.toml`. |
| `collections` | A dictionary of page collections, where each collection is a list of pages in a subdirectory. |


#### Markdown Pages

When the content of your page is mostly text, it is recommended to use Markdown for easier writing and readability. These files can still include HTML tags as needed and have access to the same scripting variables as HTML pages.

Below is an example of a Markdown page with front matter, note the use of the `include` function to include a partial and the use of variables defined in the front matter.

```markdown
---
layout = "my-template"
title = "Hello"
description = "This is the hello page description."
published = "2024-06-01"
---
{{include "page-title"}}

This is the content of the {{title}} page.

> This is a blockquote.

Lorem ipsum dolor sit amet consectetur adipisicing elit. Facere cum, officia neque eveniet iure ipsa ab dolore magnam deleniti quos dignissimos. Facilis soluta quo ut fuga illum, magnam cum aspernatur.
```

#### HTML Pages

HTML pages work similarly, but provide full control over the output. Below is an example of an HTML page with front matter:

```html
---
title = "Hello HTML"
description = "This is the hello html page description."
published = "2024-06-02"
---
<h1>{{title}}</h1>

<p>This is the content of the {{title}} page.</p>

<blockquote>
    This is a blockquote.
</blockquote>

<p>Lorem ipsum dolor sit amet consectetur adipisicing elit. Facere cum, officia neque eveniet iure ipsa ab dolore magnam deleniti quos dignissimos. Facilis soluta quo ut fuga illum, magnam cum aspernatur.</p>
```

### Page Collections

Page collections are groups of pages stored in subdirectories within the `src/` directory. Collections are derived from the first segment of a page's URL and are used to organize pages into logical groups, such as blog posts or sections of a website.

How it works:

1. Collection Derivation:

    - Collections are derived from the first segment of a page's URL path.
    - Only pages with more than one segment in their path (e.g., /posts/2023-01-01.html) are included in collections.

2. Grouping:
    - Pages are grouped by the first segment of their URL path (e.g., /posts/).

3. Sorting:
    - Pages within each collection are sorted alphabetically by their path. This ensures consistent ordering, which is useful for date-based filenames or other structured naming conventions.

Collection data is accessible via the `collections` global variable. Each collection can be accessed by its name, and it contains a list of pages within that collection.

Below is an example of a template that lists all blog posts in the `posts` collection. Note the use of the `date` module to convert and format the date for display.

```html
---
title = "Blog Posts"
description = "A list of all blog posts."
---
{{for post in collections.posts}}
    <h2><a href="{{post.path}}">{{post.title}}</a></h2>
    <p><em>{{post.data.date | date_only.to_date_time | date.to_string '%F'}}</em></p>
    <p>{{post.description}}</p>
    <hr/>
{{end}}
```

## Scripting

Scriban is a powerful templating language used in Ditto for dynamic content generation. Below is a concise overview of Scriban's syntax and features.

For a complete reference, please visit the [Scriban Language Documentation](https://github.com/scriban/scriban/tree/master/doc).

### **Basic Syntax**

- **Code Blocks**: Enclosed in `{{` and `}}`.
  ```
  {{ name = "World" }}
  Hello, {{ name }}!
  ```
  Output: `Hello, World!`

- **Text Blocks**: Plain text outside `{{` and `}}` is output as-is.

- **Comments**: Use `#` for single-line comments and `##` for multi-line comments.
  ```
  {{ # This is a comment }}
  {{ ## This is a
  multi-line comment ## }}
  ```

### **Variables**

- Declare variables with `=`.
  ```
  {{ x = 5 }}
  {{ x + 1 }}
  ```
  Output: `6`

- Special variables: `this` (current object), `empty` (empty object).

### **Control Structures**

- **If/Else**:
  ```
  {{ if x > 5 }}
    Greater than 5
  {{ else }}
    Less or equal to 5
  {{ end }}
  ```

- **Loops**:
  ```
  {{ for item in [1, 2, 3] }}
    {{ item }}
  {{ end }}
  ```
  Output: `123`

### **Functions**

- Define functions:
  ```
  {{ func add(a, b) }}
    {{ ret a + b }}
  {{ end }}
  {{ add(2, 3) }}
  ```
  Output: `5`

- Anonymous functions:
  ```
  {{ do; ret $0 + $1; end }}
  ```

### **Expressions**

- Arithmetic: `+`, `-`, `*`, `/`, `%`
- Comparisons: `==`, `!=`, `<`, `>`
- Logical: `&&`, `||`, `!`

### **Built-in Functions**

#### Array Functions

| Function Name       | Description                                                                 |
|---------------------|-----------------------------------------------------------------------------|
| `array.add`         | Adds an item to the end of an array.                                       |
| `array.add_range`   | Adds multiple items to the end of an array.                                |
| `array.compact`     | Removes all null or empty values from an array.                           |
| `array.concat`      | Concatenates two arrays into one.                                          |
| `array.cycle`       | Cycles through the elements of an array.                                  |
| `array.any`         | Returns true if any element in the array matches a condition.             |
| `array.each`        | Applies a function to each element in the array.                          |
| `array.filter`      | Filters elements in an array based on a condition.                        |
| `array.first`       | Returns the first element of an array.                                    |
| `array.insert_at`   | Inserts an element at a specific index in the array.                      |
| `array.join`        | Joins elements of an array into a string with a specified separator.      |
| `array.last`        | Returns the last element of an array.                                     |
| `array.limit`       | Limits the number of elements in an array.                                |
| `array.map`         | Maps each element of an array to a new value.                             |
| `array.offset`      | Skips a specified number of elements in an array.                         |
| `array.remove_at`   | Removes an element at a specific index in the array.                      |
| `array.reverse`     | Reverses the order of elements in an array.                               |
| `array.size`        | Returns the number of elements in an array.                               |
| `array.sort`        | Sorts the elements of an array.                                           |
| `array.uniq`        | Removes duplicate elements from an array.                                |
| `array.contains`    | Checks if an array contains a specific value.                             |

**Examples:**
```
{{ arr = [1, 2, 3] }}
{{ arr.add(4) }}
{{ arr }}
```
Output: `[1, 2, 3, 4]`

```
{{ arr = [1, null, 2, "", 3] }}
{{ arr.compact }}
```
Output: `[1, 2, 3]`

#### Date Functions

| Function Name       | Description                                                                 |
|---------------------|-----------------------------------------------------------------------------|
| `date.now`          | Returns the current date and time.                                         |
| `date.add_days`     | Adds a specified number of days to a date.                                 |
| `date.add_months`   | Adds a specified number of months to a date.                               |
| `date.add_years`    | Adds a specified number of years to a date.                                |
| `date.add_hours`    | Adds a specified number of hours to a date.                                |
| `date.add_minutes`  | Adds a specified number of minutes to a date.                              |
| `date.add_seconds`  | Adds a specified number of seconds to a date.                              |
| `date.add_milliseconds` | Adds a specified number of milliseconds to a date.                     |
| `date.parse`        | Parses a string into a date object.                                        |
| `date.parse_to_string` | Parses a string into a formatted date string.                           |
| `date.to_string`    | Converts a date object to a string using a specified format.              |

**Examples:**
```
{{ date.now }}
```
Output: `2026-01-15T12:00:00`

```
{{ date.parse("2026-01-15") | date.add_days(5) | date.to_string "%A, %B %d, %Y" }}
```
Output: `Tuesday, January 20, 2026`

#### Math Functions

| Function Name       | Description                                                                 |
|---------------------|-----------------------------------------------------------------------------|
| `math.abs`          | Returns the absolute value of a number.                                    |
| `math.ceil`         | Rounds a number up to the nearest integer.                                 |
| `math.floor`        | Rounds a number down to the nearest integer.                               |
| `math.round`        | Rounds a number to the nearest integer or specified precision.             |
| `math.plus`         | Adds two numbers.                                                          |
| `math.minus`        | Subtracts one number from another.                                         |
| `math.times`        | Multiplies two numbers.                                                    |
| `math.divided_by`   | Divides one number by another.                                             |
| `math.modulo`       | Returns the remainder of a division.                                       |
| `math.random`       | Generates a random number within a specified range.                       |
| `math.uuid`         | Generates a unique identifier (UUID).                                      |

**Examples:**
```
{{ math.abs(-42) }}
```
Output: `42`

```
{{ math.random(1, 10) }}
```
Output: `7` (randomized)

#### String Functions

| Function Name       | Description                                                                 |
|---------------------|-----------------------------------------------------------------------------|
| `string.capitalize` | Capitalizes the first letter of a string.                                  |
| `string.contains`   | Checks if a string contains a substring.                                   |
| `string.length`     | Returns the length of a string.                                            |
| `string.replace`    | Replaces occurrences of a substring with another string.                  |
| `string.split`      | Splits a string into an array based on a delimiter.                        |
| `string.strip`      | Removes leading and trailing whitespace from a string.                    |
| `string.to_int`     | Converts a string to an integer.                                           |
| `string.to_float`   | Converts a string to a floating-point number.                              |
| `string.upcase`     | Converts a string to uppercase.                                            |
| `string.downcase`   | Converts a string to lowercase.                                            |

**Examples:**
```
{{ "hello world" | string.capitalize }}
```
Output: `Hello world`

```
{{ "  hello  " | string.strip }}
```
Output: `hello`

#### DateOnly Function

Ditto provides a custom shim function `date_only.to_date_time` to facilitate working with `DateOnly` values. This function converts a `DateOnly` value into a `DateTime` object, enabling compatibility with Scriban's date functions.

Example usage:
```
{{ "2026-01-15" | date_only.to_date_time | date.to_string "%A, %B %d, %Y" }}
```
Output: `Thursday, January 15, 2026`

This shim is particularly useful when working with date values stored in TOML configuration files or front matter, ensuring seamless integration with Scriban's templating capabilities.

## Contributing

I kindly ask that before submitting a pull request, you first submit an [issue](https://github.com/pimbrouwers/Ditto/issues).

If functionality is added to the API, or changed, please kindly update the relevant documentation. Unit tests must also be added and/or updated before a pull request can be successfully merged.

Only pull requests which pass all build checks and comply with the general coding standard can be approved.

## Find a bug?

There's an [issue](https://github.com/pimbrouwers/Ditto/issues) for that.

## License

Licensed under [MIT](https://github.com/pimbrouwers/Ditto/blob/master/LICENSE).
