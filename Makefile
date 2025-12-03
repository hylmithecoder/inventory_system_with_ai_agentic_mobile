# Detect Operating System
UNAME_S := $(shell uname -s)

# Common Variables
DOTNET = dotnet
PROJECT_NAME = InventorySystem.csproj

# --- KONFIGURASI OS OTOMATIS ---
ifeq ($(UNAME_S),Darwin)
    TARGET_FRAMEWORK = net10.0-ios
    RUN_COMMAND = $(DOTNET) build $(PROJECT_NAME) -t:Run -f $(TARGET_FRAMEWORK)
    BUILD_DIR = bin/Release/$(TARGET_FRAMEWORK)
else
    TARGET_FRAMEWORK = net10.0-android
    ADB = adb
    APK_NAME = com.tunggubangbelumsiap.inventorysystem-Signed.apk
    RUN_COMMAND = $(ADB) install -r bin/Release/$(TARGET_FRAMEWORK)/$(APK_NAME) && $(ADB) shell monkey -p com.tunggubangbelumsiap.inventorysystem 1
    BUILD_DIR = bin/Release/$(TARGET_FRAMEWORK)
endif

.PHONY: all build run test clean ios android

all: build run

build:
	@echo "--- Building project for $(TARGET_FRAMEWORK) on $(UNAME_S) ---"
	$(DOTNET) build $(PROJECT_NAME) -f $(TARGET_FRAMEWORK) -c Release

run:
	@echo "--- Deploying & Running App ---"
	$(RUN_COMMAND)


android:
	@echo "--- Forcing Android Build ---"
	$(DOTNET) build $(PROJECT_NAME) -f net10.0-android -c Release
	
ios:
	@echo "--- Forcing iOS Build (Simulator) ---"
	$(DOTNET) build $(PROJECT_NAME) -t:Run -f net10.0-ios

test:
	@echo "--- Running tests ---"
	$(DOTNET) test

clean:
	@echo "--- Cleaning up ---"
	$(DOTNET) clean
	rm -rf bin/ obj/

info:
	@echo "Current OS: $(UNAME_S)"
	@echo "Target Framework: $(TARGET_FRAMEWORK)"

install:
	@echo "--- Installing dependencies ---"
	$(DOTNET) restore $(PROJECT_NAME)