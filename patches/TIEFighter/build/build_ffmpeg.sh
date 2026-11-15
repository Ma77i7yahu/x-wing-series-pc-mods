# Builds FFmpeg 8.0.1 the way the OpenX Windows releases do (packaging/windows/Dockerfile), plus swscale,
# so the mod builds link against the same avcodec-62/avformat-62/avutil-60/swresample-6/swscale-9 ABI
# as the DLLs in the release folders. Only the headers and import libraries are used; the DLLs are not shipped.
set -e
mkdir -p /g/Dev/reldeps/src
cd /g/Dev/reldeps/src
[ -d FFmpeg ] || git clone -q --branch n8.0.1 --depth 1 https://github.com/FFmpeg/FFmpeg.git FFmpeg
cd FFmpeg
./configure --prefix=/g/Dev/reldeps/prefix --target-os=mingw32 --arch=x86_64 \
  --disable-autodetect --disable-avdevice --disable-avfilter --disable-debug --disable-doc \
  --disable-everything --disable-network --disable-programs --disable-static --enable-shared \
  --enable-decoder=pcm_u8,pcm_s16le,vorbis --enable-parser=vorbis --enable-demuxer=ogg,wav \
  --enable-w32threads --disable-x86asm > ../ffcfg.log 2>&1
make -j8 > ../ffmake.log 2>&1
make install > ../ffinst.log 2>&1
ls /g/Dev/reldeps/prefix/bin /g/Dev/reldeps/prefix/lib
