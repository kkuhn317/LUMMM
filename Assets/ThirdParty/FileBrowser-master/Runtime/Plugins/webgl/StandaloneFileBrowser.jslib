mergeInto(LibraryManager.library, {
    InitializeIndexedDB: function (str) {
        window.databaseName = UTF8ToString(str);

        console.log("Database name: " + window.databaseName);

        window.selectedFiles = [];
        window.filesToSave = 0;
        window.counter = 0;
        window.databaseConnection = null;

        window.indexedDB = window.indexedDB || window.webkitIndexedDB || window.mozIndexedDB || window.OIndexedDB || window.msIndexedDB;
        window.IDBTransaction = window.IDBTransaction || window.webkitIDBTransaction || window.OIDBTransaction || window.msIDBTransaction;
        window.dbVersion = 21;

        //Inject our required html input fields
        window.InjectHiddenFileInput = function InjectHiddenFileInput(inputFieldName, acceptedExtentions, multiFileSelect) {

            //Make sure file extentions start with a dot ([.jpg,.png] instead of [jpg,png] etc)
            var acceptedExtentionsArray = acceptedExtentions.split(",");
            for (var i = 0; i < acceptedExtentionsArray.length; i++) {
                acceptedExtentionsArray[i] = "." + acceptedExtentionsArray[i].replace(".", "");
            }

            var newInput = document.createElement("input");
            newInput.id = inputFieldName;
            newInput.type = 'file';
            newInput.accept = acceptedExtentionsArray.toString();
            newInput.multiple = multiFileSelect;
            newInput.onclick = function () {
                // Reset value to null so that onChange will always trigger, even when re-uploading the same file
                this.value = null;
                SendMessage(inputFieldName, 'ClickNativeButton');
            };
            newInput.onchange = function () {
                if (this.value === null) return;

                window.ReadFiles(this.files);
            };
            newInput.style.cssText = 'display:none; cursor:pointer; opacity: 0; position: fixed; bottom: 0; left: 0; z-index: 2; width: 0px; height: 0px;';

            document.body.appendChild(newInput);
        };

        //Support for dragging dropping files on browser window
        document.addEventListener("dragover", function (event) {
            event.preventDefault();
        });

        document.addEventListener("drop", function (event) {
            console.log("File dropped");
            event.stopPropagation();
            event.preventDefault();

            // tell Unity how many files to expect
            window.ReadFiles(event.dataTransfer.files);
        });

        window.FileSaved = function FileSaved() {
            filesToSave = filesToSave - 1;
            if (filesToSave == 0) {
                window.databaseConnection.close();
            }
        };

        window.ClearInputs = function ClearInputs() {
            var inputs = document.getElementsByTagName('input');
            for (i = 0; i < inputs.length; ++i) {
                inputs[i].value = '';
            }
        };

        window.ReadFiles = function ReadFiles(SelectedFiles) {
            if (window.File && window.FileReader && window.FileList && window.Blob) {
                window.ConnectToDatabaseAndReadFiles(SelectedFiles);
                SendMessage('UserFileUploads', 'FileCount', SelectedFiles.length);
            } else {
                alert("Bestanden inladen wordt helaas niet ondersteund door deze browser.");
            }
        };

        window.ConnectToDatabaseAndReadFiles = function ConnectToDatabase(SelectedFiles) {
            //Connect to database
            window.filesToSave = SelectedFiles.length;

            var dbConnectionRequest = window.indexedDB.open("/idbfs", window.dbVersion);
            dbConnectionRequest.onsuccess = function () {
                console.log("connected to database");
                window.databaseConnection = dbConnectionRequest.result;
                for (var i = 0; i < SelectedFiles.length; i++) {
                    window.ReadFile(SelectedFiles[i])
                }
            }
            dbConnectionRequest.onerror = function () {
                alert("Kan geen verbinding maken met de indexedDatabase");
            }
        };

        window.ReadFile = function ReadFile(file) {
            window.filereader = new FileReader();
            window.filereader.onload = function (e) {
                const uint8Array = new Uint8Array(e.target.result);
                window.SaveData(uint8Array, file.name);
                window.counter = counter + 1;
            };
            window.filereader.readAsArrayBuffer(file);
        };

        window.SaveData = function SaveData(uint8Array, filename) {
            var data = {
                timestamp: new Date(),
                mode: 33206,
                contents: uint8Array
            };

            var transaction = window.databaseConnection.transaction(["FILE_DATA"], "readwrite");
            var objectStore = transaction.objectStore("FILE_DATA");

            function getUniqueFileName(objectStore, baseName, extension, callback) {
                function buildFileName(base, number, ext) {
                    return number > 0 ? `${base}(${number})${ext}` : `${base}${ext}`;
                }

                let number = 0;
                let fileName = buildFileName(baseName, number, extension);

                function checkFileName() {
                    let key = window.databaseName + "/" + fileName;
                    let request = objectStore.getKey(key);  // Use getKey to check if the file exists

                    request.onsuccess = function () {
                        if (request.result !== undefined) {
                            number++;
                            fileName = buildFileName(baseName, number, extension);
                            checkFileName();  // Check the next possible file name
                        } else {
                            callback(fileName);  // Return the unique file name when no match is found
                        }
                    };

                    request.onerror = function () {
                        console.log("Error checking file name.");
                    };
                }

                checkFileName();
            }

            let newFileName = filename;
            let fileNameWithoutExtension = filename.substring(0, filename.lastIndexOf('.'));
            let fileExtension = filename.substring(filename.lastIndexOf('.'));
            getUniqueFileName(objectStore, fileNameWithoutExtension, fileExtension, function (uniqueFileName) {
                newFileName = uniqueFileName;  // The result of the unique file name calculation

                var newIndexedFilePath = window.databaseName + "/" + newFileName;
                let dbRequest = objectStore.put(data, newIndexedFilePath);

                console.log("Saving file: " + newIndexedFilePath);
                dbRequest.onsuccess = function () {
                    SendMessage('UserFileUploads', 'LoadFile', newFileName);
                    console.log("File saved: " + newIndexedFilePath);
                    window.FileSaved();
                };
                dbRequest.onerror = function () {
                    SendMessage('UserFileUploads', 'LoadFileError', newFileName);
                    alert("Could not save: " + newIndexedFilePath);
                    window.FileSaved();
                };
            });
        };
    },

    /**
     * Can be called by Unity to open (click) the file input with the given field name.
     */
    BrowseForFile: function (inputFieldName) {
        document.getElementById(UTF8ToString(inputFieldName)).click();
    },

    UploadFromIndexedDB: function (filePath, targetURL, callbackObject, callbackMethodSuccess, callbackMethodFailed) {
        var callbackObjectString = UTF8ToString(callbackObject);
        var callbackMethodSuccessString = UTF8ToString(callbackMethodSuccess);
        var callbackMethodFailedString = UTF8ToString(callbackMethodFailed);

        console.log("Set callback object to " + callbackObjectString);
        console.log("Set callback succeeded method to " + callbackMethodSuccessString);
        console.log("Set callback failed method to " + callbackMethodFailedString);

        var fileName = UTF8ToString(filePath);
        var url = UTF8ToString(targetURL);

        var dbConnectionRequest = window.indexedDB.open("/idbfs", window.dbVersion);
        dbConnectionRequest.onsuccess = function () {
            console.log("Connected to database");
            window.databaseConnection = dbConnectionRequest.result;

            var transaction = window.databaseConnection.transaction(["FILE_DATA"], "readonly");
            var indexedFilePath = window.databaseName + "/" + fileName;
            console.log("Uploading from IndexedDB file: " + indexedFilePath);

            var dbRequest = transaction.objectStore("FILE_DATA").get(indexedFilePath);
            dbRequest.onsuccess = function (e) {
                var record = e.target.result;
                var xhr = new XMLHttpRequest;
                xhr.open("PUT", url, false);
                xhr.send(record.contents);
                window.databaseConnection.close();
                SendMessage(callbackObjectString, callbackMethodSuccessString);
            };
            dbRequest.onerror = function () {
                window.databaseConnection.close();
                SendMessage(callbackObjectString, callbackMethodFailedString, filename);
            };
        }
        dbConnectionRequest.onerror = function () {
            alert("Kan geen verbinding maken met de indexedDatabase");
        }
    },

    DownloadFromIndexedDB: function (filePath, callbackObject, callbackMethod) {
        var fileNameString = UTF8ToString(filePath);
        var callbackObjectString = UTF8ToString(callbackObject);
        var callbackMethodString = UTF8ToString(callbackMethod);

        console.log("Set callback object to " + callbackObjectString);
        console.log("Set callback method to " + callbackMethodString);

        var dbConnectionRequest = window.indexedDB.open("/idbfs", window.dbVersion);
        dbConnectionRequest.onsuccess = function () {
            console.log("Connected to database");
            window.databaseConnection = dbConnectionRequest.result;

            var transaction = window.databaseConnection.transaction(["FILE_DATA"], "readonly");
            var indexedFilePath = window.databaseName + "/" + fileNameString;
            console.log("Downloading from IndexedDB file: " + indexedFilePath);

            var dbRequest = transaction.objectStore("FILE_DATA").get(indexedFilePath);
            dbRequest.onsuccess = function (e) {
                var blob = new Blob([e.target.result.contents], {type: 'application/octetstream'});
                var url = window.URL.createObjectURL(blob);
                var onlyFileName = fileNameString.replace(/^.*[\\\/]/, '');
                const a = document.createElement("a");
                a.href = url;
                a.setAttribute("download", onlyFileName);
                document.body.appendChild(a);
                a.click();
                window.setTimeout(function () {
                    window.URL.revokeObjectURL(url);
                    document.body.removeChild(a);
                    SendMessage(callbackObjectString, callbackMethodString, fileNameString);
                }, 0);
                window.databaseConnection.close();
            };
            dbRequest.onerror = function () {
                window.databaseConnection.close();
            };
        }
        dbConnectionRequest.onerror = function () {
            alert("Kan geen verbinding maken met de indexedDatabase");
        }
    },

    AddFileInput: function (inputName, fileExtentions, multiSelect) {
        var inputNameID = UTF8ToString(inputName);
        var allowedFileExtentions = UTF8ToString(fileExtentions);

        if (typeof window.InjectHiddenFileInput !== "undefined") {
            window.InjectHiddenFileInput(inputNameID, allowedFileExtentions, multiSelect);
        } else {
            console.log("Cant create file inputfield. You need to initialize the IndexedDB connection first using InitializeIndexedDB(str)");
        }
    },

    SyncFilesFromIndexedDB: function (callbackObject, callbackMethod) {
        var callbackObjectString = UTF8ToString(callbackObject);
        var callbackMethodString = UTF8ToString(callbackMethod);
        console.log("Set callback object to " + callbackObjectString);
        console.log("Set callback method to " + callbackMethodString);

        FS.syncfs(true, function (err) {
            if (err != null) {
                console.log(err);
            }
            SendMessage(callbackObjectString, callbackMethodString);
        });
    },

    SyncFilesToIndexedDB: function (callbackObject, callbackMethod) {
        var callbackObjectString = UTF8ToString(callbackObject);
        var callbackMethodString = UTF8ToString(callbackMethod);
        console.log("Set callback object to " + callbackObjectString);
        console.log("Set callback method to " + callbackMethodString);

        FS.syncfs(false, function (err) {
            if (err != null) {
                console.log(err);
            }
            SendMessage(callbackObjectString, callbackMethodString);
        });
    },

    ClearFileInputFields: function () {
        window.ClearInputs();
    },

    // Open file.
    // gameObjectNamePtr: Unique GameObject name. Required for calling back unity with SendMessage.
    // methodNamePtr: Callback method name on given GameObject.
    // filter: Filter files. Example filters:
    //     Match all image files: "image/*"
    //     Match all video files: "video/*"
    //     Match all audio files: "audio/*"
    //     Custom: ".plist, .xml, .yaml"
    // multiselect: Allows multiple file selection
    UploadFile: function (gameObjectNamePtr, methodNamePtr, filterPtr, multiselect) {
        gameObjectName = UTF8ToString(gameObjectNamePtr);
        methodName = UTF8ToString(methodNamePtr);
        filter = UTF8ToString(filterPtr);

        // Delete if element exist
        var fileInput = document.getElementById("fileselect")
        if (fileInput) {
            document.body.removeChild(fileInput);
        }

        fileInput = document.createElement('input');
        fileInput.setAttribute('id', "fileselect");
        fileInput.setAttribute('type', 'file');
        fileInput.setAttribute('style', 'visibility:hidden;');
        if (multiselect) {
            fileInput.setAttribute('multiple', '');
        }
        if (filter) {
            fileInput.setAttribute('accept', filter);
        }
        fileInput.onclick = function (event) {
            // File dialog opened
            this.value = null;
        };
        fileInput.onchange = function (event) {
            // multiselect works
            var urls = [];
            ReadFiles(urls);


            // Remove after file selected
            // document.body.removeChild(fileInput);
        }
        document.body.appendChild(fileInput);

        document.onmouseup = function () {

            fileInput.click();

            document.onmouseup = null;
        }
    },

    // Save file
    // DownloadFile method does not open SaveFileDialog like standalone builds, its just allows user to download file
    // gameObjectNamePtr: Unique GameObject name. Required for calling back unity with SendMessage.
    // methodNamePtr: Callback method name on given GameObject.
    // filenamePtr: Filename with extension
    // byteArray: byte[]
    // byteArraySize: byte[].Length
    DownloadFile: function (gameObjectNamePtr, methodNamePtr, filenamePtr, byteArray, byteArraySize) {
        gameObjectName = UTF8ToString(gameObjectNamePtr);
        methodName = UTF8ToString(methodNamePtr);
        filename = UTF8ToString(filenamePtr);

        var bytes = new Uint8Array(byteArraySize);
        for (var i = 0; i < byteArraySize; i++) {
            bytes[i] = HEAPU8[byteArray + i];
        }

        var downloader = window.document.createElement('a');
        downloader.setAttribute('id', gameObjectName);
        downloader.href = window.URL.createObjectURL(new Blob([bytes], {type: 'application/octet-stream'}));
        downloader.download = filename;
        document.body.appendChild(downloader);

        downloader.onclick = function () {
            document.body.removeChild(downloader);
            document.onclick = null;
            SendMessage(gameObjectName, methodName, "Downloading file");
        }
        downloader.click();
    }
});