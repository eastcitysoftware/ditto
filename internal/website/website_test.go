package website

import (
	"fmt"
	"os"
	"path/filepath"
	"testing"
)

func TestLoad(t *testing.T) {
	// Test loading a website with a valid configuration
	config := &WebsiteConfig{
		PagesDir:      "testdata/pages",
		DefaultLayout: "default.tmpl",
		OutputDir:     "testdata/output",
	}

	website, err := Load(config)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if len(website.Pages) == 0 {
		t.Fatal("expected pages to be loaded, got none")
	}
	if website.OutputDir != config.OutputDir {
		t.Errorf("expected output dir %s, got %s", config.OutputDir, website.OutputDir)
	}

	if website.Layouts["default.tmpl"] == nil {
		t.Fatal("expected default layout to be loaded, got nil")
	}
	for _, page := range website.Pages {
		if _, err := filepath.Rel(config.PagesDir, page.InputPath); err != nil {
			t.Fatalf("expected no error, got %v", err)
		}
		if website.Layouts[page.Layout] == nil {
			t.Errorf("expected layout %s to be loaded, got nil", page.Layout)
		}
	}
}

func TestGetPageName(t *testing.T) {
	// Test with a valid page file path
	pageFile := fmt.Sprintf("pages/about%s", TmplExtension)
	pagesDir := "pages"
	expectedPageName := "about/index.html"

	pageName, err := getPageName(pageFile, pagesDir)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if pageName != expectedPageName {
		t.Errorf("expected %s, got %s", expectedPageName, pageName)
	}
}

func TestGetPageNameWithSubdirectory(t *testing.T) {
	// Test with a page file path that includes a subdirectory
	pageFile := fmt.Sprintf("pages/blog/posts/post1%s", TmplExtension)
	pagesDir := "pages"
	expectedPageName := "blog/posts/post1/index.html"

	pageName, err := getPageName(pageFile, pagesDir)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if pageName != expectedPageName {
		t.Errorf("expected %s, got %s", expectedPageName, pageName)
	}
}

func TestGetPageNameIndex(t *testing.T) {
	// Test with a page file path that is an index file
	pageFile := fmt.Sprintf("pages/index%s", TmplExtension)
	pagesDir := "pages"
	expectedPageName := "index.html"

	pageName, err := getPageName(pageFile, pagesDir)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if pageName != expectedPageName {
		t.Errorf("expected %s, got %s", expectedPageName, pageName)
	}
}

func TestGetFilesRecursiveNoSkips(t *testing.T) {
	dir := "testdata/pages"
	expectedFiles := map[string]bool{
		"testdata/pages/layouts/_partial.tmpl":    true,
		"testdata/pages/layouts/default.tmpl":     true,
		"testdata/pages/layouts/subpage.tmpl":     true,
		"testdata/pages/subpage/subpagepage.tmpl": true,
		"testdata/pages/index.tmpl":               true,
		"testdata/pages/page.tmpl":                true}

	files, err := getFilesRecursive(dir, nil)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if len(files) != len(expectedFiles) {
		t.Fatalf("expected %d files, got %d", len(expectedFiles), len(files))
	}
	// Check if the files match the expected files
	for _, file := range files {
		if !expectedFiles[file] {
			t.Errorf("did not expect to find %v", file)
		}
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
