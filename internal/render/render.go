package render

import (
	"encoding/json"
	"fmt"
	"html/template"
	"io"
	"strings"
)

const (
	openFrontmatterTag      = "{{/*"
	closeopenFrontmatterTag = "*/}}"
)

func RenderNamedTemplate(rd io.Reader, wr io.Writer, layout string, layoutTemplate *template.Template) error {
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
	pageTemplate := template.Must(template.Must(layoutTemplate.Clone()).Parse(pageContent))
	pageTemplate.ExecuteTemplate(wr, layout, pageData)
	return nil
}

func extractJsonFrontmatter(pageContent string) (string, map[string]any, error) {
	// if the file starts with template tag "{{/*", read until closing tag "*/}}"
	// and parse the json frontmatter

	openTagIndex := strings.Index(pageContent, openFrontmatterTag)
	if openTagIndex == 0 {
		closeTagIndex := strings.Index(pageContent, closeopenFrontmatterTag)
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
