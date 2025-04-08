package render

import (
	"encoding/json"
	"fmt"
	"html/template"
	"io"
	"os"
	"path/filepath"
	"strings"

	"github.com/eastcitysoftware/ditto/internal/website"
)

const (
	openFrontmatterTag      = "{{/*"
	closeopenFrontmatterTag = "*/}}"
)

func RenderWebsite(website website.Website) error {
	// clean output directory
	err := removeFileRecursive(website.BaseLayout, "index.html")
	if err != nil {
		return err
	}

	// parse layouts and partials
	layouts := template.Must(template.ParseFiles(website.Layouts...))

	// render pages
	for _, page := range website.Pages {
		pageReader, err := os.Open(page.InputPath)
		if err != nil {
			return fmt.Errorf("failed to open page file %s: %w", page.InputPath, err)
		}

		if err := os.MkdirAll(filepath.Dir(page.OutputPath), os.ModePerm); err != nil {
			return fmt.Errorf("failed to create output directory %s: %w", page.OutputPath, err)
		}

		outputFile, err := os.Create(page.OutputPath)
		if err != nil {
			return fmt.Errorf("failed to create output file %s: %w", page.OutputPath, err)
		}
		defer outputFile.Close()

		err = renderPage(pageReader, outputFile, website.BaseLayout, layouts)
		if err != nil {
			return fmt.Errorf("failed to render page %s: %w", page.InputPath, err)
		}
	}

	return nil
}

func renderPage(rd io.Reader, wr io.Writer, layout string, layouts *template.Template) error {
	// consume reader and get page content
	pageContentBytes, err := io.ReadAll(rd)
	if err != nil {
		return fmt.Errorf("failed to read page content: %w", err)
	}

	// extract json frontmatter from page file
	pageContent, pageData, err := extractJsonFrontmatter(string(pageContentBytes))
	if err != nil {
		return fmt.Errorf("failed to extract json frontmatter: %w", err)
	}

	// render template using layout
	pageTemplate := template.Must(template.Must(layouts.Clone()).Parse(pageContent))
	pageTemplate.ExecuteTemplate(wr, layout, pageData)
	return nil
}

func extractJsonFrontmatter(pageContent string) (string, map[string]any, error) {
	// if the file starts with template tag "{{/*", read until closing tag "*/}}"
	// and parse the json frontmatter

	openTag := "{{/*"
	closeTag := "*/}}"

	openTagIndex := strings.Index(pageContent, openTag)
	if openTagIndex == 0 {
		closeTagIndex := strings.Index(pageContent, closeTag)
		if closeTagIndex != -1 {
			frontmatter := pageContent[openTagIndex+4 : closeTagIndex]
			frontmatter = strings.TrimSpace(frontmatter)
			// parse json frontmatter
			var data map[string]any
			err := json.Unmarshal([]byte(frontmatter), &data)
			if err != nil {
				return pageContent, nil, err
			}

			return pageContent[closeTagIndex+4:], data, nil
		}
	}

	return pageContent, nil, nil
}

func removeFileRecursive(removeDir string, file string) error {
	return filepath.Walk(removeDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}

		if strings.HasSuffix(path, file) {
			// if the parent directory of the file is not "pages"
			// remove the file and its parent directory
			// otherwise remove the file only
			if dir := filepath.Dir(path); dir == removeDir {
				err := os.Remove(path)
				if err != nil {
					return err
				}
			} else {
				err := os.RemoveAll(dir)
				if err != nil {
					return err
				}
			}
		}

		return nil
	})
}
