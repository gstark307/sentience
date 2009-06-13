/*
    functions useful for file I/O
    Copyright (C) 2009 Bob Mottram
    fuzzgun@gmail.com

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public License
    as published by the Free Software Foundation; either version 2.1 of
    the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston,
    MA 02111-1307 USA
*/

#include "fileio.h"

/*!
 * \brief returns true if the given file exists
 * \param filename name of the file
 */
bool fileio::FileExists(
	char* filename)
{
    bool flag = false;
    std::fstream fin;
    printf("file exists: %s\n", filename);
    fin.open(filename, std::ios::in);
    if (fin.is_open())
        flag = true;
    fin.close();
    return(flag);
}

/*!
 * \brief returns true if the given file exists
 * \param filename name of the file
 */
bool fileio::FileExists(
	std::string filename)
{
    std::ifstream inf;

    bool flag = false;
    inf.open(filename.c_str());
    if (inf.good()) flag = true;
    inf.close();
    return(flag);
}


/*!
 * \brief returns a list of all filenames within a directory
 * \param dir directory
 * \param filenames list of filenames
 */
void fileio::GetFilesInDirectory(
	std::string dir,
	std::vector<std::string> &filenames)
{
	filenames.clear();

    DIR *dp;
    struct dirent *dirp;
    if ((dp = opendir(dir.c_str())) == NULL)
    {
    	printf("Error %d opening %s\n", errno, dir.c_str());
    	return;
    }

    while ((dirp = readdir(dp)) != NULL)
    {
    	std::string filename = dirp->d_name;
    	if (filename.size() > 3)
    	{
    		if ((filename.substr(filename.size()-3,3) == "bmp") ||
    		    (filename.substr(filename.size()-3,3) == "BMP"))
    		{
    		    //cout << "filename: " << filename << endl;
                filenames.push_back(filename);
    		}
    	}
    }
    std::sort(filenames.begin(), filenames.end());
    closedir(dp);
}

bool fileio::FileDelete(
	std::string filename)
{
	if (remove(filename.c_str()) != -1)
	    return(true);
	else
		return(false);
}

bool fileio::FileCopy(
	std::string src,
	std::string dest)
{
	int c;
	FILE *in,*out;
    in = fopen( src.c_str(), "r" );
	out = fopen( dest.c_str(), "w" );
	if ((in == NULL) || (!in))
	{
		return(false);
	}
	else
	{
		if ((out == NULL) || (!out))
	    {
		    return(false);
	    }

		while ((c = getc(in)) != EOF) putc(c,out);

	    fclose(in);
	    fclose(out);

	    return(true);
	}
}
