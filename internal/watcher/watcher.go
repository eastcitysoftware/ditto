package watcher

import (
	"fmt"
	"os"
	"path/filepath"
	"time"
)

const EventTypeCreated = "created"
const EventTypeModified = "modified"

type FileInfo struct {
	Path    string
	Size    int64
	ModTime time.Time
}

type OnChangeFunc func(fileInfo *FileInfo) error

func WatchDirectory(watchDir string, extensions []string, onChange OnChangeFunc) error {
	if _, err := os.Stat(watchDir); os.IsNotExist(err) {
		return fmt.Errorf("watch directory does not exist: %s", watchDir)
	}

	files, err := getWatchFiles(watchDir, extensions)
	if err != nil {
		return fmt.Errorf("error getting watch files: %v", err)
	}

	err = watchFiles(files, onChange)
	if err != nil {
		return fmt.Errorf("error watching files: %v", err)
	}

	return nil
}

func watchFiles(files []string, onChange OnChangeFunc) error {
	initial := map[string]*FileInfo{}

	for {
		for _, file := range files {
			fileInfo, err := getFileInfo(file)
			if err != nil {
				return fmt.Errorf("error getting file info: %v", err)
			}

			if initial[file] == nil {
				initial[file] = fileInfo
			} else {
				if initial[file].ModTime != fileInfo.ModTime || initial[file].Size != fileInfo.Size {
					onChange(fileInfo)
					initial[file] = fileInfo
				}
			}
		}

		time.Sleep(1 * time.Second)
	}
}

func getWatchFiles(watchDir string, extensionFilter []string) ([]string, error) {
	files := []string{}
	err := filepath.Walk(watchDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}
		if info.IsDir() {
			return nil
		}

		if len(extensionFilter) == 0 {
			files = append(files, path)
		} else {
			for _, ext := range extensionFilter {
				if filepath.Ext(path) == ext {
					files = append(files, path)
				}
			}
		}
		return nil
	})
	if err != nil {
		return nil, fmt.Errorf("error walking the path %v: %v", watchDir, err)
	}
	return files, nil
}

func getFileInfo(filePath string) (*FileInfo, error) {
	stat, err := os.Stat(filePath)
	if err != nil {
		return nil, fmt.Errorf("error getting file info: %v", err)
	}

	return &FileInfo{
		Path:    filepath.ToSlash(filePath),
		Size:    stat.Size(),
		ModTime: stat.ModTime(),
	}, nil
}
