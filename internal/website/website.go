package website

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

const (
	TmplExtension     = ".tmpl"
	DefaultPagesDir   = "pages"
	DefaultLayoutsDir = "layouts"
	DefaultOutputDir  = "public"
	DefaultBaseLayout = "base.tmpl"
)

type WebsiteConfig struct {
	PagesDir   string
	BaseLayout string
	OutputDir  string
}

type Website struct {
	OutputDir  string
	BaseLayout string
	Layouts    []string
	Pages      []Page
}

type Page struct {
	Name       string
	InputPath  string
	OutputPath string
}

func LoadWebsite(config *WebsiteConfig) (*Website, error) {
	// get layout files
	layoutsDir := filepath.Join(config.PagesDir, "layouts")
	layoutFiles, err := getFilesRecursive(layoutsDir, nil)
	if err != nil {
		return nil, err
	}

	// get page files
	pageFiles, err := getFilesRecursive(config.PagesDir, []string{layoutsDir})
	if err != nil {
		return nil, err
	}

	pages := []Page{}

	for _, pageFile := range pageFiles {
		pageName, err := getPageName(pageFile, config.PagesDir)

		if err != nil {
			return nil, err
		}

		// Normalize pageName to ensure it works correctly with filepath.Join
		normalizedPageName := filepath.FromSlash(pageName)

		pages = append(pages, Page{
			Name:       normalizedPageName,
			InputPath:  pageFile,
			OutputPath: filepath.Join(config.OutputDir, normalizedPageName)})
	}

	website := &Website{
		OutputDir:  config.OutputDir,
		BaseLayout: config.BaseLayout,
		Layouts:    layoutFiles,
		Pages:      pages}

	return website, nil
}

func getPageName(page string, pagesPath string) (string, error) {
	// strip .tmpl extension and add .html extension
	rel, err := filepath.Rel(pagesPath, page)
	if err != nil {
		return "", fmt.Errorf("failed to determine page name for %s: %w", page, err)
	}

	base := strings.TrimSuffix(rel, "."+filepath.Ext(rel))
	if base == "index" {
		base = base + ".html"
	} else {
		base = filepath.Join(base, "index.html")
	}

	return filepath.ToSlash(base), nil
}

// func getLayoutName(page string, layouts []string) string {
// 	// if layouts contains a template with the same name as the page
// 	// directory, use that template
// 	layoutName := "default.tmpl"
// 	if pageDir := filepath.Base(filepath.Dir(page)); pageDir != "pages" {
// 		for _, layoutFile := range layouts {
// 			if strings.HasPrefix(filepath.Base(layoutFile), pageDir) {
// 				layoutName = pageDir + ".tmpl"
// 			}
// 		}
// 	}
// 	return layoutName
// }

func getFilesRecursive(dir string, skipDirs []string) ([]string, error) {
	var fileNames []string
	skipMap := make(map[string]bool)
	for _, skipDir := range skipDirs {
		skipMap[skipDir] = true
	}

	err := filepath.WalkDir(dir, func(file string, d os.DirEntry, err error) error {
		if err != nil {
			return fmt.Errorf("failed to walk files in '%s': %w", dir, err)
		}

		// skip directories
		if d.IsDir() {
			// skip files if path starts with any of the skipDirs
			if skipMap[file] {
				return filepath.SkipDir
			}
			return nil
		}

		// skip files that do not have the .tmpl extension
		if filepath.Ext(file) != TmplExtension {
			return nil
		}

		fileNames = append(fileNames, filepath.ToSlash(file))

		return nil
	})

	return fileNames, err
}
