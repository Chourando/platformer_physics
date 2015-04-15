#!/bin/bash

if [[ -z $1 ]] ; then
	echo "Usage: $0 <file> <folder> [dpi]"
	exit 0
else
	if [[ -z $2 ]] ; then
		echo "Usage: $0 <file> <folder> [dpi]"
		exit 0
	else
		FILENAME=$1
		FOLDER=$2

		if [[ -z $3 ]] ; then
			DPI=90
		else
			DPI=$3
		fi
	fi
fi

INKSCAPE="\"C:/Program Files (x86)/Inkscape/inkscape.exe\""

PREFIX=EXP-

for ID in `grep -o "id=\"$PREFIX.*\"" $FILENAME | cut -d\" -f2` ; do
	OUTPUT=${FOLDER}/${ID#$PREFIX}.png
	echo "Exporting area $ID to $OUTPUT..."
	eval $INKSCAPE --export-id=$ID --export-png=$OUTPUT --export-dpi=$DPI --file=$FILENAME
done