package main

import (
	"flag"
	"log"
	"os"

	"github.com/eastcitysoftware/ditto/internal/server"
	"github.com/eastcitysoftware/ditto/internal/watcher"
	"github.com/eastcitysoftware/ditto/internal/website"
)

func main() {
	// parse command line flags
	root := flag.String("root", "", "root directory of the project")
	port := flag.Int("port", 8080, "port to run the server on")
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

	// create the website config
	config, err := website.NewConfig(*root)
	if err != nil {
		log.Fatalf("failed to create page config: %v", err)
	}

	// load the website from disk
	site, err := website.Load(config)
	if err != nil {
		log.Fatalf("failed to load website: %v", err)
	}
	log.Println("loaded website with", len(site.Pages), "pages")

	// render the website to disk
	log.Println("rendering pages to", site.OutputDir)
	err = website.Render(site, "")
	if err != nil {
		log.Fatalf("rendering pages failed with %v", err)
	}

	log.Println("watching for changes in", config.PagesDir)
	go watcher.WatchDirectory(
		config.PagesDir,
		[]string{website.TmplExtension},
		func(fileInfo *watcher.FileInfo) error {
			log.Printf("File changed: %s", fileInfo.Path)
			website.Render(site, fileInfo.Path)
			return nil
		})

	// start the development server
	log.Println("starting development server on port", *port)
	err = server.StartDevelopmentServer(*port, config.OutputDir)
	if err != nil {
		log.Fatalf("failed to start server: %v", err)
	} else {
		log.Print("gracefully stopped the server")
	}
}
