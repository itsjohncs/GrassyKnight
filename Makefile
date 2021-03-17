.PHONY: .FORCE clean

bin/Debug/GrassyKnight.dll: .FORCE
	msbuild /nowarn:MSB3277 GrassyKnight.csproj

GrassyKnight.zip: zip-build/README.md zip-build/ALL_KEYBOARD_KEY_NAMES.txt zip-build/screenshot.png zip-build/LICENSE zip-build/GrassyKnight.dll zip-build/VERSION
	cd zip-build && zip ../GrassyKnight.zip $(^F)

zip-build:
	mkdir -p zip-build

zip-build/GrassyKnight.dll: bin/Debug/GrassyKnight.dll
	cp $< $@

zip-build/README.md: README.md zip-build
	./add_link_to_readme.py README.md > $@

zip-build/VERSION: GrassyKnight.cs zip-build
	grep 'public override string GetVersion() => "' GrassyKnight.cs | grep -Eo '[0-9]+\.[0-9]+\.[0-9]+' > $@

zip-build/ALL_KEYBOARD_KEY_NAMES.txt: ALL_KEYBOARD_KEY_NAMES.txt zip-build
	cp $< $@

zip-build/screenshot.png: screenshot.png zip-build
	cp $< $@

zip-build/LICENSE: LICENSE zip-build
	cp $< $@

clean:
	rm -rf zip-build/ bin/ obj/ GrassyKnight.zip
