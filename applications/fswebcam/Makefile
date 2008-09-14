# Makefile for fswebcam
# [22/02/2006]

SHELL = /bin/sh

prefix      = /usr/local
exec_prefix = ${prefix}
bindir      = ${exec_prefix}/bin
mandir      = ${prefix}/man

CC      = gcc
CFLAGS  = -g -O2 -DHAVE_CONFIG_H
LDFLAGS = -lgd 

OBJS = fswebcam.o log.o effects.o parse.o src.o src_test.o src_raw.o src_file.o src_v4l1.o src_v4l2.o
#       src_test.o src_file.o src_v4l1.o src_v4l2.o

all: fswebcam fswebcam.1.gz

install: all
	mkdir -p ${DESTDIR}${bindir}
	mkdir -p ${DESTDIR}${mandir}/man1
	install -m 755 fswebcam ${DESTDIR}${bindir}
	install -m 644 fswebcam.1.gz ${DESTDIR}${mandir}/man1

fswebcam: $(OBJS)
	$(CC) $(LDFLAGS) $(OBJS) -o fswebcam

.c.o:
	${CC} ${CFLAGS} -c $< -o $@

fswebcam.1.gz: fswebcam.1
	gzip -c --best fswebcam.1 > fswebcam.1.gz

clean:
	rm -f core* *.o fswebcam fswebcam.1.gz

distclean: clean
	rm -rf config.h *.cache config.log config.status Makefile *.jp*g *.png

