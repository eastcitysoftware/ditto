package watcher

import (
	"testing"
)

func TestGetWatchFiles(t *testing.T) {
	testWatchDir := "testdata"
	testExtensionFilter := []string{".txt"}
	testWatchFiles, err := getWatchFiles(testWatchDir, testExtensionFilter)
	if err != nil {
		t.Fatalf("Error getting watch files: %v", err)
	}
	if len(testWatchFiles) != 8 {
		t.Errorf("Expected 8 watch files, but got %d", len(testWatchFiles))
	}
}

func TestGetFileChangeInfo(t *testing.T) {
	testFilePath := "testdata/a.txt"
	testFileInfo, err := getFileInfo(testFilePath)
	if err != nil {
		t.Fatalf("Error getting file info: %v", err)
	}
	if testFileInfo.Path != testFilePath {
		t.Errorf("Expected file path %s, but got %s", testFilePath, testFileInfo.Path)
	}
	if testFileInfo.Size != 0 {
		t.Errorf("Expected an empty file, but got %d", testFileInfo.Size)
	}
}
