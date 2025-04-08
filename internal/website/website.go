package website

import (
	"fmt"
	"html/template"
	"os"
	"path/filepath"
	"strings"

	"github.com/eastcitysoftware/ditto/internal/render"
)

const (
	TmplExtension     = ".tmpl"
	DefaultPagesDir   = "pages"
	DefaultLayoutsDir = "layouts"
	DefaultOutputDir  = "public"
	DefaultLayout     = "default.tmpl"
)

type WebsiteConfig struct {
	PagesDir      string
	DefaultLayout string
	OutputDir     string
}

type Website struct {
	OutputDir string
	Layouts   map[string]*template.Template
	Pages     []Page
}

type Page struct {
	Name       string
	Layout     string
	InputPath  string
	OutputPath string
}

func Render(website *Website) error {
	// clean output directory
	err := removeFileRecursive(website.OutputDir, "index.html")
	if err != nil {
		return err
	}

	// render pages
	for _, page := range website.Pages {
		if website.Layouts[page.Layout] == nil {
			return fmt.Errorf("layout %s not found for page %s", page.Layout, page.InputPath)
		}

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

		err = render.RenderPage(pageReader, outputFile, page.Layout, website.Layouts[page.Layout])
		if err != nil {
			return fmt.Errorf("failed to render page %s: %w", page.InputPath, err)
		}
	}

	return nil
}

func Load(config *WebsiteConfig) (*Website, error) {
	// get layout files
	layoutsDir := filepath.Join(config.PagesDir, "layouts")
	allLayoutfiles, err := getFilesRecursive(layoutsDir, nil)
	if err != nil {
		return nil, err
	}

	// separate layouts from partials
	layoutFiles := []string{}
	partialFiles := []string{}
	for _, layoutFile := range allLayoutfiles {
		filename := filepath.Base(layoutFile)
		if strings.HasPrefix(filename, "_") {
			partialFiles = append(partialFiles, layoutFile)
		} else {
			layoutFiles = append(layoutFiles, layoutFile)
		}
	}

	// build layout map
	layouts := map[string]*template.Template{}
	for _, layoutFile := range layoutFiles {
		layoutName := filepath.Base(layoutFile)
		layout, err := template.ParseFiles(append(partialFiles, layoutFile)...)
		if err != nil {
			return nil, fmt.Errorf("failed to parse layout file %s: %w", layoutFile, err)
		}
		layouts[layoutName] = layout
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

		// check if layout exists for page
		layout := DefaultLayout
		layoutFromFile := filepath.Base(pageFile)
		if _, exists := layouts[layoutFromFile]; exists {
			layout = layoutFromFile
		}

		layoutFromParent := filepath.Base(filepath.Dir(pageFile)) + TmplExtension
		if _, exists := layouts[layoutFromParent]; exists {
			layout = layoutFromParent
		}

		pages = append(pages, Page{
			Name:       normalizedPageName,
			Layout:     layout,
			InputPath:  pageFile,
			OutputPath: filepath.Join(config.OutputDir, normalizedPageName)})
	}

	website := &Website{
		OutputDir: config.OutputDir,
		Layouts:   layouts,
		Pages:     pages}

	return website, nil
}

func NewConfig(root string) (*WebsiteConfig, error) {
	// establish and check directories
	outputDir := filepath.Join(root, DefaultOutputDir)
	_, err := os.Stat(outputDir)
	if err != nil {
		return nil, fmt.Errorf("public directory does not exist")
	}

	pagesPath := filepath.Join(root, DefaultPagesDir)
	_, err = os.Stat(pagesPath)
	if err != nil {
		return nil, fmt.Errorf("pages directory does not exist")
	}

	layoutsDir := filepath.Join(pagesPath, DefaultLayoutsDir)
	_, err = os.Stat(layoutsDir)
	if err != nil {
		return nil, fmt.Errorf("layouts directory does not exist")
	}

	config := &WebsiteConfig{
		PagesDir:      pagesPath,
		DefaultLayout: DefaultLayout,
		OutputDir:     outputDir,
	}
	return config, nil
}

func getPageName(page string, pagesPath string) (string, error) {
	// strip .tmpl extension and add .html extension
	rel, err := filepath.Rel(pagesPath, page)
	if err != nil {
		return "", fmt.Errorf("failed to determine page name for %s: %w", page, err)
	}
	base := strings.TrimSuffix(rel, filepath.Ext(rel))

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
