# checkimports.sh <exe> <release dir>: every DLL the exe imports must be in the release folder
# (or be a Windows system DLL), and must export every name the exe imports from it.
exe="$1"; rel="$2"; bad=0
exports() { objdump -p "$1" | awk '/^\[Ordinal\/Name Pointer\] Table/ {t=1; next} t && /^\t\[/ {print $NF} t && /^$/ {t=0}' | sort -u; }
objdump -p "$exe" | awk '
  /^\tDLL Name: / {dll=$3; next}
  dll && /^\t[0-9a-f]+  <none>  [0-9a-f]+  / {print dll "\t" $4}
  /^$/ {dll=""}' > /tmp/imports.$$
for dll in $(cut -f1 /tmp/imports.$$ | sort -u); do
  path=$(find "$rel" -maxdepth 1 -iname "$dll" | head -1)
  if [ -z "$path" ]; then
    if [ -f "/c/Windows/System32/$dll" ] || [ -f "/c/Windows/System32/$(echo $dll | tr A-Z a-z)" ]; then echo "system  $dll"; continue; fi
    echo "MISSING $dll (not in the release folder)"; bad=1; continue
  fi
  exports "$path" > /tmp/exports.$$
  missing=$(grep -P "^\Q$dll\E\t" /tmp/imports.$$ | cut -f2 | sort -u | comm -23 - /tmp/exports.$$)
  n=$(grep -cP "^\Q$dll\E\t" /tmp/imports.$$)
  if [ -n "$missing" ]; then echo "MISSING from $dll: $(echo $missing | tr '\n' ' ')"; bad=1; else echo "ok      $dll ($n imports)"; fi
done
rm -f /tmp/imports.$$ /tmp/exports.$$
exit $bad
