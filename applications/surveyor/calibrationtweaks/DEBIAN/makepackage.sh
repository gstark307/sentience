cd ..
rm -r temp
mkdir temp
mkdir temp/DEBIAN
mkdir temp/usr
mkdir temp/usr/bin
mkdir temp/usr/bin/sentience
mkdir temp/usr/bin/sentience/stereotweaks
cp DEBIAN/control temp/DEBIAN
cp DEBIAN/copyright temp/DEBIAN
cp bin/Debug/*.* temp/usr/bin/sentience/stereotweaks
dpkg -b temp DEBIAN/stereotweaks.deb
cd DEBIAN
alien -r stereotweaks.deb
