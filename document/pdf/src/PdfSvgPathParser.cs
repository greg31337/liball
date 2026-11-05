using System;
using System.IO;

namespace pdf;

internal static class PdfSvgPathParser
{
    internal static PdfPath Parse(string data)
    {
        if (data.Length == 0)
        {
            throw new InvalidDataException("SVG path data cannot be empty.");
        }

        PdfSvgNumberReader reader = new PdfSvgNumberReader(data);
        PdfPath path = new PdfPath();
        char command = (char)0;
        char previousCommand = (char)0;
        double currentX = 0;
        double currentY = 0;
        double subpathX = 0;
        double subpathY = 0;
        double cubicControlX = 0;
        double cubicControlY = 0;
        double quadraticControlX = 0;
        double quadraticControlY = 0;

        while (reader.HasMore())
        {
            if (!reader.NextIsNumber())
            {
                command = reader.ReadCommand();
            }
            else if (command == (char)0)
            {
                throw new InvalidDataException("SVG path data requires a command before its coordinates.");
            }

            bool relative = command >= 'a' && command <= 'z';
            char normalized = char.ToUpperInvariant(command);
            if (normalized == 'Z')
            {
                path.Close();
                currentX = subpathX;
                currentY = subpathY;
                previousCommand = normalized;
                command = (char)0;
                continue;
            }

            if (normalized == 'A')
            {
                throw new InvalidDataException("SVG elliptical arc path commands are not supported by this subset.");
            }

            if (normalized == 'M')
            {
                double x = reader.ReadNumber();
                double y = reader.ReadNumber();
                if (relative)
                {
                    x = x + currentX;
                    y = y + currentY;
                }

                path.MoveTo(x, y);
                currentX = x;
                currentY = y;
                subpathX = x;
                subpathY = y;
                previousCommand = 'M';
                while (reader.NextIsNumber())
                {
                    x = reader.ReadNumber();
                    y = reader.ReadNumber();
                    if (relative)
                    {
                        x = x + currentX;
                        y = y + currentY;
                    }

                    path.LineTo(x, y);
                    currentX = x;
                    currentY = y;
                    previousCommand = 'L';
                }

                command = relative ? 'l' : 'L';
                continue;
            }

            bool consumed = false;
            while (reader.NextIsNumber())
            {
                consumed = true;
                if (normalized == 'L')
                {
                    double x = reader.ReadNumber();
                    double y = reader.ReadNumber();
                    if (relative)
                    {
                        x = x + currentX;
                        y = y + currentY;
                    }

                    path.LineTo(x, y);
                    currentX = x;
                    currentY = y;
                }
                else if (normalized == 'H')
                {
                    double x = reader.ReadNumber();
                    if (relative)
                    {
                        x = x + currentX;
                    }

                    path.LineTo(x, currentY);
                    currentX = x;
                }
                else if (normalized == 'V')
                {
                    double y = reader.ReadNumber();
                    if (relative)
                    {
                        y = y + currentY;
                    }

                    path.LineTo(currentX, y);
                    currentY = y;
                }
                else if (normalized == 'C')
                {
                    double x1 = reader.ReadNumber();
                    double y1 = reader.ReadNumber();
                    double x2 = reader.ReadNumber();
                    double y2 = reader.ReadNumber();
                    double x = reader.ReadNumber();
                    double y = reader.ReadNumber();
                    if (relative)
                    {
                        x1 = x1 + currentX;
                        y1 = y1 + currentY;
                        x2 = x2 + currentX;
                        y2 = y2 + currentY;
                        x = x + currentX;
                        y = y + currentY;
                    }

                    path.CurveTo(x1, y1, x2, y2, x, y);
                    cubicControlX = x2;
                    cubicControlY = y2;
                    currentX = x;
                    currentY = y;
                }
                else if (normalized == 'S')
                {
                    double x1 = currentX;
                    double y1 = currentY;
                    if (previousCommand == 'C' || previousCommand == 'S')
                    {
                        x1 = currentX * 2 - cubicControlX;
                        y1 = currentY * 2 - cubicControlY;
                    }

                    double x2 = reader.ReadNumber();
                    double y2 = reader.ReadNumber();
                    double x = reader.ReadNumber();
                    double y = reader.ReadNumber();
                    if (relative)
                    {
                        x2 = x2 + currentX;
                        y2 = y2 + currentY;
                        x = x + currentX;
                        y = y + currentY;
                    }

                    path.CurveTo(x1, y1, x2, y2, x, y);
                    cubicControlX = x2;
                    cubicControlY = y2;
                    currentX = x;
                    currentY = y;
                }
                else if (normalized == 'Q')
                {
                    double controlX = reader.ReadNumber();
                    double controlY = reader.ReadNumber();
                    double x = reader.ReadNumber();
                    double y = reader.ReadNumber();
                    if (relative)
                    {
                        controlX = controlX + currentX;
                        controlY = controlY + currentY;
                        x = x + currentX;
                        y = y + currentY;
                    }

                    double x1 = currentX + (controlX - currentX) * 2 / 3;
                    double y1 = currentY + (controlY - currentY) * 2 / 3;
                    double x2 = x + (controlX - x) * 2 / 3;
                    double y2 = y + (controlY - y) * 2 / 3;
                    path.CurveTo(x1, y1, x2, y2, x, y);
                    quadraticControlX = controlX;
                    quadraticControlY = controlY;
                    currentX = x;
                    currentY = y;
                }
                else if (normalized == 'T')
                {
                    double controlX = currentX;
                    double controlY = currentY;
                    if (previousCommand == 'Q' || previousCommand == 'T')
                    {
                        controlX = currentX * 2 - quadraticControlX;
                        controlY = currentY * 2 - quadraticControlY;
                    }

                    double x = reader.ReadNumber();
                    double y = reader.ReadNumber();
                    if (relative)
                    {
                        x = x + currentX;
                        y = y + currentY;
                    }

                    double x1 = currentX + (controlX - currentX) * 2 / 3;
                    double y1 = currentY + (controlY - currentY) * 2 / 3;
                    double x2 = x + (controlX - x) * 2 / 3;
                    double y2 = y + (controlY - y) * 2 / 3;
                    path.CurveTo(x1, y1, x2, y2, x, y);
                    quadraticControlX = controlX;
                    quadraticControlY = controlY;
                    currentX = x;
                    currentY = y;
                }
                else
                {
                    throw new InvalidDataException("SVG path command is unsupported.");
                }

                previousCommand = normalized;
            }

            if (!consumed)
            {
                throw new InvalidDataException("SVG path command is missing required coordinates.");
            }
        }

        if (!path.HasCommands())
        {
            throw new InvalidDataException("SVG path contains no drawable commands.");
        }

        return path;
    }
}
