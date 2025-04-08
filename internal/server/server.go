package server

import (
	"fmt"
	"net/http"
)

func StartWebServer(port int, dir string) error {
	s := &http.Server{
		Addr:    fmt.Sprintf("localhost:%d", port),
		Handler: http.FileServer(http.Dir(dir))}
	return s.ListenAndServe()
}
