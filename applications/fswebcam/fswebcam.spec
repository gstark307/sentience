#
# Written 22-09-2004 <phil@firestorm.cx>
#

Summary: Tiny and flexible webcam program
Name: fswebcam
Version: 20070108
Release: 1
License: GPL
Group: Applications/Multimedia
Source: http://www.firestorm.cx/fswebcam/files/fswebcam-%{version}.tar.gz
URL: http://www.firestorm.cx/fswebcam/
BuildRequires: gd-devel > 2
Requires: gd > 2
BuildRoot: %{_tmppath}/%{name}-%{version}-%{release}-root-%(%{__id_u} -n)

%description
A tiny and flexible webcam program for capturing images from a V4L1/V4L2
device, and overlaying a caption or image.

%prep
%setup -q

%build
%configure
make %{?_smp_mflags}

%install
rm -rf %{buildroot}
make DESTDIR="%{buildroot}" install

%clean
rm -rf %{buildroot}

%files
%defattr(-,root,root)
%doc README CHANGELOG LICENSE example.conf
%{_bindir}/fswebcam
%{_mandir}/man1/fswebcam.1.gz

%changelog
* Sun Dec 10 2006 Philip Heron <phil@firestorm.cx> - 20061210-1
- Added example configuration.

* Fri Apr 28 2006 Philip Heron <phil@firestorm.cx> - 20060424-1
- Updated package description, and group.

* Wed Feb 22 2006 Philip Heron <phil@firestorm.cx>
- Updated spec to use configure script and cleaned up.
