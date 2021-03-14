#!/usr/bin/env python3

import sys

# This could be done with sed, but there's some relevant differences between
# mac's sed and linux's sed and it just doesn't feel worth it.
with open(sys.argv[1]) as f:
    for index, line in enumerate(f):
        if index == 2:
            sys.stdout.write(
                "For a nicely formatted version of this README, go to "
                "https://github.com/itsjohncs/GrassyKnight#grassy-knight\n\n")

        sys.stdout.write(line)
