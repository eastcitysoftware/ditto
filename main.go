package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"path/filepath"

	"github.com/eastcitysoftware/ditto/internal/render"
	// "github.com/eastcitysoftware/ditto/internal/server"
	"github.com/eastcitysoftware/ditto/internal/website"
)

func main() {
	// parse command line flags
	root := flag.String("root", "", "root directory of the project")
	// port := flag.Int("port", 8080, "port to run the server on")
	flag.Parse()

	// show usage if no arguments are provided
	if len(os.Args) <= 1 {
		flag.Usage()
		os.Exit(0)
	}

	// if there is a single argument and no root is specified, use the first
	// argument as the root
	if len(os.Args) == 2 {
		if *root == "" {
			*root = os.Args[1]
		}
	}

	config, err := newPageConfig(*root)
	if err != nil {
		log.Fatalf("failed to create page config: %v", err)
	}

	website, err := website.LoadWebsite(config)
	if err != nil {
		log.Fatalf("failed to load website: %v", err)
	}
	log.Println("loaded website with", len(website.Pages), "pages")

	log.Println("rendering pages to", website.OutputDir)
	err = render.RenderWebsite(*website)
	if err != nil {
		log.Fatalf("rendering pages failed with %v", err)
	}

	// server := newDevelopmentServer(*port, config.outputDir)
	// log.Println("starting server on ", server.addr)
	// server.start()
}

func newPageConfig(root string) (*website.WebsiteConfig, error) {
	// establish and check directories
	outputDir := filepath.Join(root, "public")
	_, err := os.Stat(outputDir)
	if err != nil {
		return nil, fmt.Errorf("public directory does not exist")
	}

	pagesPath := filepath.Join(root, "pages")
	_, err = os.Stat(pagesPath)
	if err != nil {
		return nil, fmt.Errorf("pages directory does not exist")
	}

	layoutsDir := filepath.Join(pagesPath, "layouts")
	_, err = os.Stat(layoutsDir)
	if err != nil {
		return nil, fmt.Errorf("layouts directory does not exist")
	}

	config := &website.WebsiteConfig{
		PagesDir:   pagesPath,
		LayoutsDir: layoutsDir,
		OutputDir:  outputDir,
	}
	return config, nil
}
