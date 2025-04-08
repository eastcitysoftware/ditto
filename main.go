package main

import (
	"context"
	"flag"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

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
	err = website.Render(site)
	if err != nil {
		log.Fatalf("rendering pages failed with %v", err)
	}

	// start the development server
	done := make(chan os.Signal, 1)
	signal.Notify(done, os.Interrupt, syscall.SIGINT, syscall.SIGTERM)

	addr := fmt.Sprintf("localhost:%d", *port)
	srv := newDevelopmentServer(addr, config.OutputDir)
	log.Println("starting server on", addr)
	go func() {
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("listen: %s\n", err)
		}
	}()
	log.Println("server started successfully")

	<-done
	log.Println("server stopped")

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer func() {
		// extra handling here
		cancel()
	}()
	if err := srv.Shutdown(ctx); err != nil {
		log.Fatalf("server Shutdown Failed:%+v", err)
	}
	log.Print("server Exited Properly")
}

func newDevelopmentServer(addr string, dir string) *http.Server {
	return &http.Server{
		Addr:    addr,
		Handler: http.FileServer(http.Dir(dir))}
}
