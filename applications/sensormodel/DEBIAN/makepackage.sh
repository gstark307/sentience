cd ..
rm -r temp
mkdir temp
mkdir temp/DEBIAN
mkdir temp/usr
mkdir temp/usr/bin
cp DEBIAN/control-core temp/DEBIAN/control
cp DEBIAN/copyright temp/DEBIAN
cp bin/Debug/*.dll temp/usr/bin/
cp bin/Debug/*.dll.mdb temp/usr/bin/
dpkg -b temp DEBIAN/sentience-core.deb
cd DEBIAN
alien -r sentience-core.deb

cd ..
rm -r temp
mkdir temp
mkdir temp/DEBIAN
mkdir temp/usr
mkdir temp/usr/bin
cp DEBIAN/control temp/DEBIAN
cp DEBIAN/copyright temp/DEBIAN
cp bin/Debug/stereosensormodel.* temp/usr/bin/
dpkg -b temp DEBIAN/stereosensormodel.deb
cd DEBIAN
alien -r stereosensormodel.deb
