using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Caps.Types;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.HLE.HOS.Services.Caps
{
    class CaptureManager
    {
        private readonly string _sdCardPath;

        private uint _shimLibraryVersion;

        public CaptureManager(Switch device)
        {
            _sdCardPath = FileSystem.VirtualFileSystem.GetSdCardPath();
        }

        public ResultCode SetShimLibraryVersion(ServiceCtx context)
        {
            ulong shimLibraryVersion = context.RequestData.ReadUInt64();

#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong appletResourceUserId = context.RequestData.ReadUInt64();
#pragma warning restore IDE0059

            // TODO: Service checks if the pid is present in an internal list
            // and returns ResultCode.BlacklistedPid if it is.
            // The list contents needs to be determined.

            ResultCode resultCode = ResultCode.OutOfRange;

            if (shimLibraryVersion != 0)
            {
                if (_shimLibraryVersion == shimLibraryVersion)
                {
                    resultCode = ResultCode.Success;
                }
                else if (_shimLibraryVersion != 0)
                {
                    resultCode = ResultCode.ShimLibraryVersionAlreadySet;
                }
                else if (shimLibraryVersion == 1)
                {
                    resultCode = ResultCode.Success;
                    _shimLibraryVersion = 1;
                }
            }

            return resultCode;
        }

        public ResultCode SaveScreenShot(
            byte[] screenshotData,
            ulong appletResourceUserId,
            ulong titleId,
            out ApplicationAlbumEntry applicationAlbumEntry)
        {
            applicationAlbumEntry = default;

            if (screenshotData == null || screenshotData.Length == 0)
            {
                return ResultCode.NullInputBuffer;
            }

            if (screenshotData.Length < 0x384000)
            {
                return ResultCode.NullInputBuffer;
            }

            DateTime currentDateTime = DateTime.Now;

            applicationAlbumEntry = new ApplicationAlbumEntry()
            {
                Size = (ulong)Unsafe.SizeOf<ApplicationAlbumEntry>(),
                TitleId = titleId,

                AlbumFileDateTime = new AlbumFileDateTime()
                {
                    Year = (ushort)currentDateTime.Year,
                    Month = (byte)currentDateTime.Month,
                    Day = (byte)currentDateTime.Day,
                    Hour = (byte)currentDateTime.Hour,
                    Minute = (byte)currentDateTime.Minute,
                    Second = (byte)currentDateTime.Second,
                    UniqueId = 0,
                },

                AlbumStorage = AlbumStorage.Sd,
                ContentType = ContentType.Screenshot,
                Padding = new Array5<byte>(),
                Unknown0x1f = 1,
            };

            /*
             * iOS / MeloNX workaround
             *
             * MeloNX currently attempts to encode screenshots using SkiaSharp.
             * The iOS package does not contain libSkiaSharp.dylib, causing:
             *
             * System.DllNotFoundException: libSkiaSharp
             *
             * when games invoke the Switch screenshot service.
             *
             * For now, emulate a successful screenshot operation without
             * encoding or writing an image.
             *
             * This allows games such as Animal Crossing: New Horizons to use
             * their camera/photo mode without terminating MeloNX.
             */

            return ResultCode.Success;
        }
    }
}
