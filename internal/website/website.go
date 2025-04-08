package website

import (
	"os"
	"path/filepath"
	"strings"
)

type WebsiteConfig struct {
	PagesDir   string
	LayoutsDir string
	OutputDir  string
}

type Website struct {
	OutputDir string
	Layouts   []string
	Pages     []Page
}

type Page struct {
	Name       string
	Layout     string
	InputPath  string
	OutputPath string
}

func LoadWebsite(config *WebsiteConfig) (*Website, error) {
	// get layout files
	layoutFiles, err := getFilesRecursive(config.LayoutsDir, []string{})
	if err != nil {
		return nil, err
	}

	// get page files
	pageFiles, err := getFilesRecursive(config.PagesDir, []string{"layouts"})
	if err != nil {
		return nil, err
	}

	pages := []Page{}

	for _, pageFile := range pageFiles {
		pageName, err := getPageName(pageFile, config.PagesDir)

		if err != nil {
			return nil, err
		}

		pages = append(pages, Page{
			Name:       pageName,
			Layout:     getLayoutName(pageFile, layoutFiles),
			InputPath:  pageFile,
			OutputPath: filepath.Join(config.OutputDir, pageName)})
	}

	website := &Website{
		OutputDir: config.OutputDir,
		Layouts:   layoutFiles,
		Pages:     pages}

	return website, nil
}

func getPageName(page string, pagesPath string) (string, error) {
	// strip .tmpl extension and add .html extension
	rel, err := filepath.Rel(pagesPath, page)

	if err != nil {
		return "", err
	}

	rel = strings.Replace(rel, filepath.Ext(rel), "", 1)
	if filepath.Base(rel) != "index" {
		rel = filepath.Join(rel, "index")
	}
	return rel + ".html", nil
}

func getLayoutName(page string, layouts []string) string {
	// if layouts contains a template with the same name as the page
	// directory, use that template
	layoutName := "default.tmpl"
	if pageDir := filepath.Base(filepath.Dir(page)); pageDir != "pages" {
		for _, layoutFile := range layouts {
			if strings.HasPrefix(filepath.Base(layoutFile), pageDir) {
				layoutName = pageDir + ".tmpl"
			}
		}
	}
	return layoutName
}

func getFilesRecursive(dir string, skipDirs []string) ([]string, error) {
	var fileNames []string

	err := filepath.WalkDir(dir, func(file string, d os.DirEntry, err error) error {
		if err != nil {
			return err
		}

		// skip directories
		if d.IsDir() {
			return nil
		}

		// skip files if path starts with dir
		skipFile := false
		for _, skipDir := range skipDirs {
			if strings.HasPrefix(file, filepath.Join(dir, skipDir)) {
				skipFile = true
				break
			}
		}

		if !skipFile {
			fileNames = append(fileNames, file)
		}

		return nil
	})

	return fileNames, err
}
