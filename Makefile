.PHONY: .FORCE

bin/Debug/GrassPls.dll: .FORCE
	msbuild /nowarn:MSB3277 GrassPls.csproj
