package render

import (
	"html/template"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestRenderPage(t *testing.T) {
	testLayout := `{{define "base.tmpl"}}<html>{{block "content" .}}{{end}}</html>{{end}}`

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
	layout := "base.tmpl"
	layouts := template.Must(template.New("").Parse(testLayout))

	err := renderPage(pageReader, pageWriter, layout, layouts)
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

func TestRemoveFileRecursive(t *testing.T) {
	tmpDir := t.TempDir()
	testFiles := []string{
		"test1.txt",
		"/test2/test.txt",
		"/test3/test.txt",
		"/test4/test.txt"}

	for _, file := range testFiles {
		err := os.MkdirAll(filepath.Join(tmpDir, file), os.ModePerm)
		if err != nil {
			t.Fatalf("failed to create test file %s: %v", file, err)
		}
		os.Create(filepath.Join(tmpDir, file))
		if err != nil {
			t.Fatalf("failed to create test file %s: %v", file, err)
		}
	}

	// Check if the files are create
	filesAfterCreate, err := os.ReadDir(tmpDir)
	if len(filesAfterCreate) != len(testFiles) {
		t.Fatalf("expected %d files after create, got %d", len(testFiles), len(filesAfterCreate))
	}
	if err != nil {
		t.Fatalf("failed to read directory after create %s: %v", tmpDir, err)
	}

	// recursively remove test.txt
	removeFileRecursive(tmpDir, "test.txt")

	// Check if the files are removed
	filesAfterRemove, err := os.ReadDir(tmpDir)
	if err != nil {
		t.Fatalf("failed to read directory after remove %s: %v", tmpDir, err)
	}
	if len(filesAfterRemove) != 1 {
		t.Fatalf("expected 1 file after delete, got %d", len(filesAfterRemove))
	}
}
