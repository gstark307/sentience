# Copyright 1999-2005 Gentoo Foundation
# Distributed under the terms of the GNU General Public License v2
# $Header: $

inherit eutils

DESCRIPTION="Tiny and flexible webcam app"
HOMEPAGE="http://www.firestorm.cx/fswebcam/"
SRC_URI="http://www.firestorm.cx/fswebcam/files/${P}.tar.gz"

LICENSE="GPL-2"
SLOT="0"
KEYWORDS="x86 amd64 arm ~ppc"
IUSE="jpeg png truetype v4l mmap"

DEPEND=">=media-libs/gd-2"

src_compile() {
        econf || die "econf failed"
        emake || die "emake failed"
}

src_install() {
	dobin fswebcam || die
	dodoc README CHANGELOG LICENSE example.conf
	doman fswebcam.1.gz
}
