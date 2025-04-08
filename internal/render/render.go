package render

import (
	"encoding/json"
	"html/template"
	"os"
	"path/filepath"
	"strings"

	"github.com/eastcitysoftware/ditto/internal/website"
)

func RenderWebsite(website website.Website) error {
	// clean output directory
	err := cleanOutputDir(website.OutputDir)
	if err != nil {
		return err
	}

	// parse layouts and partials
	layouts := template.Must(template.ParseFiles(website.Layouts...))

	// render pages
	for _, page := range website.Pages {
		err := renderPage(page, layouts)
		if err != nil {
			return err
		}
	}

	return nil
}

func renderPage(page website.Page, layouts *template.Template) error {
	pageContentBytes, err := os.ReadFile(page.InputPath)
	if err != nil {
		return err
	}

	// extract json frontmatter from page file
	pageContent, pageData, err := extractJsonFrontmatter(string(pageContentBytes))
	if err != nil {
		return err
	}

	// render template using layout
	pageTemplate := template.Must(template.Must(layouts.Clone()).Parse(pageContent))
	out, err := prepareOutFile(page.OutputPath)
	if err != nil {
		return err
	}
	defer out.Close()

	pageTemplate.ExecuteTemplate(out, "default.tmpl", pageData)
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

func cleanOutputDir(outputDir string) error {
	err := removeFileRecursive(outputDir, "index.html")
	if err != nil {
		return err
	}
	return nil
}

func removeFileRecursive(removeDir string, file string) error {
	return filepath.Walk(removeDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}
		if info.IsDir() {
			return nil
		}

		if strings.HasSuffix(path, file) {
			// if the parent directory of the file is not "pages"
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

func prepareOutFile(outFile string) (*os.File, error) {
	if err := os.MkdirAll(filepath.Dir(outFile), os.ModePerm); err != nil {
		return nil, err
	}

	return os.Create(outFile)
}
