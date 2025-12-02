# Define variables for common commands and project name
DOTNET = dotnet
PROJECT_NAME = InventorySystem.csproj
BUILD_DIR = bin/Release/net10.0-android # Adjust target framework as needed
ADB = adb
APK_NAME = com.tunggubangbelumsiap.inventorysystem-Signed.apk

.PHONY: all build run test clean

all: build run

build:
	@echo "--- Building the .NET project ---"
	$(DOTNET) build $(PROJECT_NAME) -f net10.0-android  -c Release

install:
	@echo "--- Installing the app on the connected Android device ---"
	$(ADB) install -r bin/Release/net10.0-android/$(APK_NAME)

test:
	@echo "--- Running tests for the .NET project ---"
	$(DOTNET) test

clean:
	@echo "--- Cleaning up build artifacts ---"
	$(DOTNET) clean
	rm -rf $(BUILD_DIR)

# Example of a non-dotnet command to demonstrate flexibility
list:
	@echo "--- Listing project files ---"
	ls -F
