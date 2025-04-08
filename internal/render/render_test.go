package render

import (
	"html/template"
	"strings"
	"testing"
)

func TestRenderPage(t *testing.T) {
	testLayout := `<html>{{block "content" .}}{{end}}</html>`

	testTemplate := `{{/*
		{
			"title": "Test Page",
			"description": "This is a test page",
			"tags": ["test", "example"]
		}
	*/}}
	{{define "content"}}
<div>{{.title}}</div>
<div>{{.description}}</div>
{{range .tags}}<div>{{.}}</div>
{{end}}{{end}}`
	pageReader := strings.NewReader(testTemplate)
	pageWriter := &strings.Builder{}
	layout := template.Must(template.New("test.tmpl").Parse(testLayout))

	err := RenderPage(pageReader, pageWriter, "test.tmpl", layout)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}

	output := pageWriter.String()
	expectedOutput := `<html>
<div>Test Page</div>
<div>This is a test page</div>
<div>test</div>
<div>example</div>
</html>`
	if output != expectedOutput {
		t.Errorf("expected %s, got %s", expectedOutput, output)
	}
}

func TestExtractJsonFrontmatter(t *testing.T) {
	pageContent := `{{/* {"title": "Test Page", "description": "This is a test page"} */}}`
	expectedTitle := "Test Page"
	expectedDescription := "This is a test page"
	_, extractedData, err := extractJsonFrontmatter(pageContent)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if extractedData["title"] != expectedTitle {
		t.Errorf("expected title %s, got %s", expectedTitle, extractedData["title"])
	}
	if extractedData["description"] != expectedDescription {
		t.Errorf("expected description %s, got %s", expectedDescription, extractedData["description"])
	}
}

func TestExtractJsonFrontmatterNewLines(t *testing.T) {
	pageContent := `{{/*
	{
		"title": "Test Page",
		"description": "This is a test page",
		"tags": ["test", "example"]
	}
*/}}
{{define "content"}}{{end}}`
	expectedRemainingContent := `
{{define "content"}}{{end}}`
	expectedTitle := "Test Page"
	expectedDescription := "This is a test page"
	remainingContent, extractedData, err := extractJsonFrontmatter(pageContent)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if remainingContent != expectedRemainingContent {
		t.Errorf("expected remaining content %s, got %s", expectedRemainingContent, remainingContent)
	}
	if extractedData["title"] != expectedTitle {
		t.Errorf("expected title %s, got %s", expectedTitle, extractedData["title"])
	}
	if extractedData["description"] != expectedDescription {
		t.Errorf("expected description %s, got %s", expectedDescription, extractedData["description"])
	}
}

func TestExtractJsonFrontmatterNoFrontmatter(t *testing.T) {
	pageContent := `{{block "test" .}}{{end}}`
	remainingContent, _, err := extractJsonFrontmatter(pageContent)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if remainingContent != pageContent {
		t.Errorf("expected remaining content to be unchanged, got %s", remainingContent)
	}
}

func TestExtractJsonFrontmatterEmpty(t *testing.T) {
	pageContent := ``
	remainingContent, _, err := extractJsonFrontmatter(pageContent)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if remainingContent != pageContent {
		t.Errorf("expected remaining content to be unchanged, got %s", remainingContent)
	}
}
