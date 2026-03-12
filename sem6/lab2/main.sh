#!/bin/bash

OPTION=""
if [[ "$1" == -* && "$1" != "-h" && "$1" != "--help" ]]; then
    OPTION="$1"
    shift
fi

if [[ "$1" == "-h" || "$1" == "--help" || $# -lt 1 ]]; then
    echo "Usage: $0 [option] [file1] [file2] ..."
    echo "Options: -n (name), -q (quantity), -t (total price)"
    exit 0
fi

printf "%-20s | %10s | %10s | %12s\n" "Name" "Quantity" "Avg. price" "Total"
echo "------------------------------------------------------------"

awk -F ',[[:space:]]*' '
BEGIN {
    format = "%-20s | %10.2f | %10.2f | %12.2f\n"
}
{
    if (NF < 3) next;
    name = $1; qty = $2; price = $3
    count[name] += qty
    total[name] += (qty * price)
}
END {
    for (i in count) {
        printf format, i, count[i], total[i]/count[i], total[i]
    }
}' "$@" | {
    case "$OPTION" in
        -n) sort -t'|' -k1,1 ;;
        -q) sort -t'|' -k2,2rn ;;
        -t) sort -t'|' -k4,4rn ;;
        *)  cat ;;
    esac
}
