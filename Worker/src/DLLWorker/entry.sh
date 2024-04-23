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
if [ "$ret" = 139 ]; then
  echo "$@" "CRASHED"
fi

# Forward the status code
exit "$ret"
