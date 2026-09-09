# ============================================================
#  CloudSync build script (GNU Make)
#  Usage: make [release|debug|clean|deploy]
# ============================================================

CONFIG    ?= Release
DOTNET     = dotnet
ROOT      := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))..
DLLS       = StardewModdingAPI.dll 0Harmony.dll MonoGame.Framework.dll Stardew\ Valley.dll SMAPI.Toolkit.CoreInterfaces.dll

.PHONY: all release debug clean deploy check

all: release

check:
	@missing=""; \
	for dll in $(DLLS); do \
		[ -f "$(ROOT)/$$dll" ] || missing="$$missing $$dll"; \
	done; \
	if [ -n "$$missing" ]; then \
		echo "[ERROR] Missing DLLs:$$missing"; \
		echo "Copy them from your Stardew Valley game folder. See README.md."; \
		exit 1; \
	fi

release: CONFIG=Release
release: check
	$(DOTNET) build $(ROOT)/CloudSync.csproj -c Release -v minimal

debug: CONFIG=Debug
debug: check
	$(DOTNET) build $(ROOT)/CloudSync.csproj -c Debug -v minimal

clean:
	$(DOTNET) build $(ROOT)/CloudSync.csproj -t:Clean -c Release
	$(DOTNET) build $(ROOT)/CloudSync.csproj -t:Clean -c Debug

deploy: release
	@echo "Copy bin/Release/net6.0/CloudSync.dll to your Mods/CloudSync/ folder"
