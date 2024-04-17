#! /bin/sh

# Enable coredumps
ulimit -c unlimited

# Rip child processes
trap 'kill -s INT -- -$$' INT
trap 'kill -s TERM -- -$$' TERM

# Launch command in background to be sure it will be ripped
"$@" &
wait $!
ret="$?"

# Add log that process has crashed
if [ "$ret" != 0 ]; then
  echo "$@" "CRASHED with status code $ret" >&2
fi

# Forward the status code
exit "$ret"
