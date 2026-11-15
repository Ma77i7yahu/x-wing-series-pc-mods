# build_rel.sh <Repo> <cmake target> <Exe> <release dir> [extra cmake args...]
# Builds the modded port in <repo>/build-rel so it runs on the DLLs that ship in the upstream
# release: FFmpeg 8.0.1 headers/import libs from /g/Dev/reldeps/prefix, SpeexDSP/curl import libs
# made from the release DLLs, libstdc++/libgcc/winpthread linked statically (the release's are older).
# Then strips the exe into /g/Dev/<Repo>-rel/ and checks every import against the release folder.
set -e
R=$1; T=$2; EXE=$3; REL=$4; shift 4
export PATH=/g/Dev/hosttools/bin:$PATH
export PKG_CONFIG_PATH=/g/Dev/reldeps/pc:/g/Dev/reldeps/prefix/lib/pkgconfig
cd /g/Dev/$R
LOG=/g/Dev/reldeps/$R
# the default Windows libraries, then winpthread statically (the release's DLL lacks nanosleep64).
# Static winpthread pulls in kernel32's GetTickCount import stub, which clashes with the port's own
# GetTickCount (host_clock.c); --allow-multiple-definition keeps the port's, as with the DLL. That
# clash is the only duplicate the link reports without the flag.
WINLIBS="-lkernel32 -luser32 -lgdi32 -lwinspool -lshell32 -lole32 -loleaut32 -luuid -lcomdlg32 -ladvapi32 -Wl,-Bstatic -lstdc++ -lwinpthread -Wl,-Bdynamic -Wl,--allow-multiple-definition"
cmake -S . -B build-rel -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_FLAGS="-g -Wno-error=incompatible-pointer-types" \
  -DCMAKE_CXX_STANDARD=17 -DCMAKE_CXX_FLAGS="-g -include wchar.h -include new -include cstdint -include cstring" \
  -DCMAKE_EXE_LINKER_FLAGS="-static-libgcc -static-libstdc++" \
  -DCMAKE_C_STANDARD_LIBRARIES="$WINLIBS" -DCMAKE_CXX_STANDARD_LIBRARIES="$WINLIBS" \
  -DCURL_NO_CURL_CMAKE=ON -DCURL_LIBRARY=G:/Dev/reldeps/implib/libcurl.dll.a -DCURL_LIBRARY_RELEASE=G:/Dev/reldeps/implib/libcurl.dll.a \
  -DCURL_INCLUDE_DIR=G:/msys64/mingw64/include \
  -DAERON_SHADERCROSS_EXECUTABLE=/g/Dev/hosttools/bin/shadercross.exe "$@" > $LOG-cfg.log 2>&1 || { tail -40 $LOG-cfg.log; exit 1; }
cmake --build build-rel --target $T > $LOG-build.log 2>&1 || { grep -n -B2 -A8 "error" $LOG-build.log | head -80; exit 1; }
# every FFmpeg header must come from the 8.0.1 prefix, never MSYS2's FFmpeg 9
if ninja -C build-rel -t deps | grep -E "mingw64/include/(libav|libsw)" | head -3 | grep .; then echo "MSYS2 FFmpeg headers were used"; exit 1; fi
echo "ffmpeg headers: $(ninja -C build-rel -t deps | grep -c 'reldeps/prefix/include/libav')"
OUT=/g/Dev/$R-rel
rm -rf $OUT; mkdir -p $OUT
strip -o $OUT/$EXE build-rel/$EXE
cp build-rel/$EXE /g/Dev/symbols/${EXE%.exe}-rel-$(date +%Y%m%d%H%M).exe
bash /g/Dev/reldeps/checkimports.sh $OUT/$EXE "$REL"
ls -la $OUT/$EXE
