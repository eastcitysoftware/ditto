package website

import (
	"fmt"
	"testing"
)

func TestLoadWebsite(t *testing.T) {
	// Test loading a website with a valid configuration
	config := &WebsiteConfig{
		PagesDir:   "testdata/pages",
		BaseLayout: "base.tmpl",
		OutputDir:  "testdata/output",
	}

	website, err := LoadWebsite(config)
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if len(website.Pages) == 0 {
		t.Fatal("expected pages to be loaded, got none")
	}
	if website.OutputDir != config.OutputDir {
		t.Errorf("expected output dir %s, got %s", config.OutputDir, website.OutputDir)
	}
}

func TestGetPageName(t *testing.T) {
	// Test with a valid page file path
	pageFile := fmt.Sprintf("pages/about.%s", TmplExtension)
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
	pageFile := fmt.Sprintf("pages/blog/posts/post1.%s", TmplExtension)
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
	pageFile := fmt.Sprintf("pages/index.%s", TmplExtension)
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
		"testdata/pages/layouts/base.tmpl":        true,
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
