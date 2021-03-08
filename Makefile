.PHONY: .FORCE

bin/Debug/GrassyKnight.dll: .FORCE
	msbuild /nowarn:MSB3277 GrassyKnight.csproj
