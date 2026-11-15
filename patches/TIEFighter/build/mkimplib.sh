# mkimplib.sh <dll> <out .dll.a>: import library for a release DLL, from its export table.
set -e
dll="$1"; out="$2"; name=$(basename "$dll")
def="${out%.a}.def"
{
  echo "LIBRARY \"$name\""
  echo "EXPORTS"
  objdump -p "$dll" | awk '/^\[Ordinal\/Name Pointer\] Table/ {t=1; next} t && /^\t\[/ {print $NF} t && /^$/ {t=0}'
} > "$def"
dlltool -d "$def" -D "$name" -l "$out"
echo "$out: $(($(wc -l < "$def") - 2)) exports"
