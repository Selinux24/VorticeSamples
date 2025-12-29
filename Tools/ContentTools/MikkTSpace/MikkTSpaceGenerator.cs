using System;
using System.Diagnostics;
using System.Numerics;

namespace ContentTools.MikkTSpace
{
    public abstract class MikkTSpaceGenerator<T>(T mesh) where T : class
    {
        const int MARK_DEGENERATE = 1;
        const int QUAD_ONE_DEGEN_TRI = 2;
        const int GROUP_WITH_ANY = 4;
        const int ORIENT_PRESERVING = 8;

        const uint INTERNAL_RND_SORT_SEED = 39871946;

        const int MAX_CELLS = 2048;

        readonly T context = mesh;

        protected T GetMesh() => context;

        public abstract int GetNumFaces();
        public abstract int GetNumVerticesOfFace(int faceIndex);
        public abstract Vector3 GetPosition(int faceIndex, int vertIndex);
        public abstract Vector3 GetNormal(int faceIndex, int vertIndex);
        public abstract Vector2 GetTexCoord(int faceIndex, int vertIndex);
        public abstract void SetTSpaceBasic(Vector3 tangent, float sign, int faceIndex, int vertIndex);

        Vector3 GetPosition(int index)
        {
            IndexToData(index, out int iF, out int iI);
            return GetPosition(iF, iI);
        }
        Vector3 GetNormal(int index)
        {
            IndexToData(index, out int iF, out int iI);
            return GetNormal(iF, iI);
        }
        Vector2 GetTexCoord(int index)
        {
            IndexToData(index, out int iF, out int iI);
            return GetTexCoord(iF, iI);
        }

        static uint Randomize(uint seed)
        {
            uint t = seed & 31u;
            t = (seed << (int)t) | (seed >> (int)(32u - t));
            return seed + t + 3;
        }

        /// <summary>
        /// Default (recommended) angular threshold is 180 degrees (which means threshold disabled)
        /// </summary>
        public bool GenTangSpace(float angularThreshold = 180f)
        {
            // count nr_triangles
            int iNrFaces = GetNumFaces();
            float fThresCos = MathF.Cos(angularThreshold * MathF.PI / 180.0f);

            // count triangles on supported faces
            int iNrTrianglesIn = 0;
            for (int f = 0; f < iNrFaces; f++)
            {
                int verts = GetNumVerticesOfFace(f);
                if (verts == 3) iNrTrianglesIn++;
                else if (verts == 4) iNrTrianglesIn += 2;
            }
            if (iNrTrianglesIn <= 0)
            {
                return false;
            }

            // allocate memory for an index list
            int[] piTriListIn = new int[iNrTrianglesIn * 3];
            TriInfo[] pTriInfos = new TriInfo[iNrTrianglesIn];
            for (int i = 0; i < iNrTrianglesIn; i++)
            {
                pTriInfos[i] = new();
            }

            // make an initial triangle --> face index list
            int iNrTSPaces = GenerateInitialVerticesIndexList(pTriInfos, piTriListIn, iNrTrianglesIn);

            // make a welded index list of identical positions and attributes (pos, norm, texc)
            //printf("gen welded index list begin\n");
            GenerateSharedVerticesIndexList(piTriListIn, iNrTrianglesIn);
            //printf("gen welded index list end\n");

            // Mark all degenerate triangles
            int iTotTris = iNrTrianglesIn;
            int iDegenTriangles = 0;
            for (int t = 0; t < iTotTris; t++)
            {
                int i0 = piTriListIn[(t * 3) + 0];
                int i1 = piTriListIn[(t * 3) + 1];
                int i2 = piTriListIn[(t * 3) + 2];
                var p0 = GetPosition(i0);
                var p1 = GetPosition(i1);
                var p2 = GetPosition(i2);
                if (p0 == p1 || p0 == p2 || p1 == p2)  // degenerate
                {
                    pTriInfos[t].Flag |= MARK_DEGENERATE;
                    iDegenTriangles++;
                }
            }
            iNrTrianglesIn = iTotTris - iDegenTriangles;

            // mark all triangle pairs that belong to a quad with only one
            // good triangle. These need special treatment in DegenEpilogue().
            // Additionally, move all good triangles to the start of
            // pTriInfos[] and piTriListIn[] without changing order and
            // put the degenerate triangles last.
            DegenPrologue(pTriInfos, piTriListIn, iNrTrianglesIn, iTotTris);

            // evaluate triangle level attributes and neighbor list
            //printf("gen neighbors list begin\n");
            InitTriInfo(pTriInfos, piTriListIn, iNrTrianglesIn);
            //printf("gen neighbors list end\n");

            int[] piGroupTrianglesBuffer = new int[iNrTrianglesIn * 3];

            // based on the 4 rules, identify groups based on connectivity
            int iNrMaxGroups = iNrTrianglesIn * 3;
            Group[] groups = new Group[iNrMaxGroups];
            for (int g = 0; g < iNrMaxGroups; g++)
            {
                groups[g] = new(piGroupTrianglesBuffer);
            }

            //printf("gen 4rule groups begin\n");
            int iNrActiveGroups = Build4RuleGroups(pTriInfos, groups, piTriListIn, iNrTrianglesIn);
            //printf("gen 4rule groups end\n");

            TSpace[] sTspace = new TSpace[iNrTSPaces];
            for (int t = 0; t < iNrTSPaces; t++)
            {
                sTspace[t] = new TSpace();

                sTspace[t].Os.X = 1.0f;
                sTspace[t].Os.Y = 0.0f;
                sTspace[t].Os.Z = 0.0f;
                sTspace[t].MagS = 1.0f;

                sTspace[t].Ot.X = 0.0f;
                sTspace[t].Ot.Y = 1.0f;
                sTspace[t].Ot.Z = 0.0f;
                sTspace[t].MagT = 1.0f;
            }

            // make tspaces, each group is split up into subgroups if necessary
            // based on fAngularThreshold. Finally a tangent space is made for
            // every resulting subgroup
            //printf("gen tspaces begin\n");
            bool bRes = GenerateTSpaces(sTspace, pTriInfos, groups, iNrActiveGroups, piTriListIn, fThresCos);
            //printf("gen tspaces end\n");

            if (!bRes)  // if an allocation in GenerateTSpaces() failed
            {
                return false;
            }

            // degenerate quads with one good triangle will be fixed by copying a space from
            // the good triangle to the coinciding vertex.
            // all other degenerate triangles will just copy a space from any good triangle
            // with the same welded index in piTriListIn[].
            DegenEpilogue(sTspace, pTriInfos, piTriListIn, iNrTrianglesIn, iTotTris);

            int index = 0;
            for (int f = 0; f < iNrFaces; f++)
            {
                int verts = GetNumVerticesOfFace(f);
                if (verts != 3 && verts != 4) continue;

                // I've decided to let degenerate triangles and group-with-anythings
                // vary between left/right hand coordinate systems at the vertices.
                // All healthy triangles on the other hand are built to always be either or.

                // set data
                for (int i = 0; i < verts; i++)
                {
                    var ts = sTspace[index++];
                    SetTSpaceBasic(ts.Os, ts.Sign, f, i);
                }
            }

            return true;
        }
        int GenerateInitialVerticesIndexList(TriInfo[] triInfos, int[] triList, int nTriangles)
        {
            int iTSpacesOffs = 0;
            int iDstTriIndex = 0;
            for (int f = 0; f < GetNumFaces(); f++)
            {
                int verts = GetNumVerticesOfFace(f);
                if (verts != 3 && verts != 4) continue;

                triInfos[iDstTriIndex].OrgFaceNumber = f;
                triInfos[iDstTriIndex].TSpacesOffs = iTSpacesOffs;

                if (verts == 3)
                {
                    triInfos[iDstTriIndex].VertNum[0] = 0;
                    triInfos[iDstTriIndex].VertNum[1] = 1;
                    triInfos[iDstTriIndex].VertNum[2] = 2;
                    triList[(iDstTriIndex * 3) + 0] = MakeIndex(f, 0);
                    triList[(iDstTriIndex * 3) + 1] = MakeIndex(f, 1);
                    triList[(iDstTriIndex * 3) + 2] = MakeIndex(f, 2);
                    iDstTriIndex++; // next
                }
                else
                {
                    triInfos[iDstTriIndex + 1].OrgFaceNumber = f;
                    triInfos[iDstTriIndex + 1].TSpacesOffs = iTSpacesOffs;

                    // need an order independent way to evaluate
                    // tspace on quads. This is done by splitting
                    // along the shortest diagonal.
                    int i0 = MakeIndex(f, 0);
                    int i1 = MakeIndex(f, 1);
                    int i2 = MakeIndex(f, 2);
                    int i3 = MakeIndex(f, 3);
                    var t0 = GetTexCoord(i0);
                    var t1 = GetTexCoord(i1);
                    var t2 = GetTexCoord(i2);
                    var t3 = GetTexCoord(i3);
                    float distSQ_02 = Vector2.Subtract(t2, t0).LengthSquared();
                    float distSQ_13 = Vector2.Subtract(t3, t1).LengthSquared();
                    bool bQuadDiagIs_02;
                    if (distSQ_02 < distSQ_13)
                    {
                        bQuadDiagIs_02 = true;
                    }
                    else if (distSQ_13 < distSQ_02)
                    {
                        bQuadDiagIs_02 = false;
                    }
                    else
                    {
                        var p0 = GetPosition(i0);
                        var p1 = GetPosition(i1);
                        var p2 = GetPosition(i2);
                        var p3 = GetPosition(i3);
                        distSQ_02 = Vector3.Subtract(p2, p0).LengthSquared();
                        distSQ_13 = Vector3.Subtract(p3, p1).LengthSquared();

                        bQuadDiagIs_02 = distSQ_13 >= distSQ_02;
                    }

                    if (bQuadDiagIs_02)
                    {
                        triInfos[iDstTriIndex].VertNum[0] = 0;
                        triInfos[iDstTriIndex].VertNum[1] = 1;
                        triInfos[iDstTriIndex].VertNum[2] = 2;
                        triList[(iDstTriIndex * 3) + 0] = i0;
                        triList[(iDstTriIndex * 3) + 1] = i1;
                        triList[(iDstTriIndex * 3) + 2] = i2;
                        iDstTriIndex++; // next

                        triInfos[iDstTriIndex].VertNum[0] = 0;
                        triInfos[iDstTriIndex].VertNum[1] = 2;
                        triInfos[iDstTriIndex].VertNum[2] = 3;
                        triList[(iDstTriIndex * 3) + 0] = i0;
                        triList[(iDstTriIndex * 3) + 1] = i2;
                        triList[(iDstTriIndex * 3) + 2] = i3;
                        iDstTriIndex++; // next
                    }
                    else
                    {
                        triInfos[iDstTriIndex].VertNum[0] = 0;
                        triInfos[iDstTriIndex].VertNum[1] = 1;
                        triInfos[iDstTriIndex].VertNum[2] = 3;
                        triList[(iDstTriIndex * 3) + 0] = i0;
                        triList[(iDstTriIndex * 3) + 1] = i1;
                        triList[(iDstTriIndex * 3) + 2] = i3;
                        iDstTriIndex++; // next

                        triInfos[iDstTriIndex].VertNum[0] = 1;
                        triInfos[iDstTriIndex].VertNum[1] = 2;
                        triInfos[iDstTriIndex].VertNum[2] = 3;
                        triList[(iDstTriIndex * 3) + 0] = i1;
                        triList[(iDstTriIndex * 3) + 1] = i2;
                        triList[(iDstTriIndex * 3) + 2] = i3;
                        iDstTriIndex++; // next
                    }
                }

                iTSpacesOffs += verts;
                Debug.Assert(iDstTriIndex <= nTriangles);
            }

            for (int t = 0; t < nTriangles; t++)
            {
                triInfos[t].Flag = 0;
            }

            // return total amount of tspaces
            return iTSpacesOffs;
        }
        void GenerateSharedVerticesIndexList(int[] triList, int nTriangles)
        {
            // Generate bounding box
            var vMin = GetPosition(0);
            var vMax = vMin;
            for (int i = 1; i < (nTriangles * 3); i++)
            {
                int index = triList[i];

                var vP = GetPosition(index);
                if (vMin.X > vP.X) vMin.X = vP.X;
                else if (vMax.X < vP.X) vMax.X = vP.X;
                if (vMin.Y > vP.Y) vMin.Y = vP.Y;
                else if (vMax.Y < vP.Y) vMax.Y = vP.Y;
                if (vMin.Z > vP.Z) vMin.Z = vP.Z;
                else if (vMax.Z < vP.Z) vMax.Z = vP.Z;
            }

            var vDim = Vector3.Subtract(vMax, vMin);
            int iChannel = 0;
            float fMin = vMin.X;
            float fMax = vMax.X;
            if (vDim.Y > vDim.X && vDim.Y > vDim.Z)
            {
                iChannel = 1;
                fMin = vMin.Y;
                fMax = vMax.Y;
            }
            else if (vDim.Z > vDim.X)
            {
                iChannel = 2;
                fMin = vMin.Z;
                fMax = vMax.Z;
            }

            // count amount of elements in each cell unit
            int[] piHashCount = new int[MAX_CELLS];
            for (int i = 0; i < (nTriangles * 3); i++)
            {
                int index = triList[i];
                var vP = GetPosition(index);
                float fVal = iChannel == 0 ? vP.X : (iChannel == 1 ? vP.Y : vP.Z);
                int iCell = FindGridCell(fMin, fMax, fVal);
                piHashCount[iCell]++;
            }

            // evaluate start index of each cell.
            int[] piHashOffsets = new int[MAX_CELLS];
            piHashOffsets[0] = 0;
            for (int k = 1; k < MAX_CELLS; k++)
            {
                piHashOffsets[k] = piHashOffsets[k - 1] + piHashCount[k - 1];
            }

            // insert vertices
            int[] piHashTable = new int[nTriangles * 3];
            int[] piHashCount2 = new int[MAX_CELLS];
            for (int i = 0; i < (nTriangles * 3); i++)
            {
                int index = triList[i];
                var vP = GetPosition(index);
                float fVal = iChannel == 0 ? vP.X : (iChannel == 1 ? vP.Y : vP.Z);
                int iCell = FindGridCell(fMin, fMax, fVal);

                Debug.Assert(piHashCount2[iCell] < piHashCount[iCell]);
                piHashTable[piHashOffsets[iCell] + piHashCount2[iCell]] = i;    // vertex i has been inserted.
                piHashCount2[iCell]++;
            }

            for (int k = 0; k < MAX_CELLS; k++)
            {
                Debug.Assert(piHashCount2[k] == piHashCount[k]);  // verify the count
            }

            // find maximum amount of entries in any hash entry
            int iMaxCount = piHashCount[0];
            for (int k = 1; k < MAX_CELLS; k++)
            {
                if (iMaxCount < piHashCount[k])
                {
                    iMaxCount = piHashCount[k];
                }
            }

            // complete the merge
            TmpVert[] pTmpVert = new TmpVert[iMaxCount];
            for (int k = 0; k < MAX_CELLS; k++)
            {
                // extract table of cell k and amount of entries in it
                int iEntries = piHashCount[k];
                if (iEntries < 2) continue;

                for (int e = 0; e < iEntries; e++)
                {
                    int i = piHashTable[piHashOffsets[k] + e];
                    var vP = GetPosition(triList[i]);
                    pTmpVert[e] = new()
                    {
                        Vert = vP,
                        Index = i
                    };
                }
                MergeVertsFast(triList, pTmpVert, 0, iEntries - 1);
            }
        }
        void MergeVertsFast(int[] triList, TmpVert[] tmpVert, int iLIn, int iRIn)
        {
            // make bbox
            Vector3 fvMin = new();
            Vector3 fvMax = new();
            for (int c = 0; c < 3; c++)
            {
                fvMin[c] = tmpVert[iLIn].Vert[c];
                fvMax[c] = fvMin[c];
            }

            for (int l = iLIn + 1; l <= iRIn; l++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (fvMin[c] > tmpVert[l].Vert[c]) fvMin[c] = tmpVert[l].Vert[c];
                    if (fvMax[c] < tmpVert[l].Vert[c]) fvMax[c] = tmpVert[l].Vert[c];
                }
            }

            float dx = fvMax[0] - fvMin[0];
            float dy = fvMax[1] - fvMin[1];
            float dz = fvMax[2] - fvMin[2];

            int channel = 0;
            if (dy > dx && dy > dz) channel = 1;
            else if (dz > dx) channel = 2;

            float fSep = 0.5f * (fvMax[channel] + fvMin[channel]);

            // stop if all vertices are NaNs
            if (float.IsNaN(fSep)) return;

            // terminate recursion when the separation/average value
            // is no longer strictly between fMin and fMax values.
            if (fSep >= fvMax[channel] || fSep <= fvMin[channel])
            {
                // complete the weld
                for (int l = iLIn; l <= iRIn; l++)
                {
                    int i = tmpVert[l].Index;
                    int index = triList[i];
                    var vP = GetPosition(index);
                    var vN = GetNormal(index);
                    var vT = GetTexCoord(index);

                    bool found = false;
                    int l2 = iLIn;
                    int i2rec = -1;
                    while (l2 < l && !found)
                    {
                        int i2 = tmpVert[l2].Index;
                        int index2 = triList[i2];
                        var vP2 = GetPosition(index2);
                        var vN2 = GetNormal(index2);
                        var vT2 = GetTexCoord(index2);
                        i2rec = i2;

                        if (vP.X == vP2.X && vP.Y == vP2.Y && vP.Z == vP2.Z &&
                            vN.X == vN2.X && vN.Y == vN2.Y && vN.Z == vN2.Z &&
                            vT.X == vT2.X && vT.Y == vT2.Y)
                        {
                            found = true;
                        }
                        else
                        {
                            l2++;
                        }
                    }

                    // merge if previously found
                    if (found)
                    {
                        triList[i] = triList[i2rec];
                    }
                }
            }
            else
            {
                Debug.Assert((iRIn - iLIn) > 0);    // at least 2 entries

                // separate (by fSep) all points between iL_in and iR_in in pTmpVert[]
                int iL = iLIn;
                int iR = iRIn;
                while (iL < iR)
                {
                    bool bReadyLeftSwap = false;
                    while ((!bReadyLeftSwap) && iL < iR)
                    {
                        Debug.Assert(iL >= iLIn && iL <= iRIn);
                        bReadyLeftSwap = !(tmpVert[iL].Vert[channel] < fSep);
                        if (!bReadyLeftSwap) ++iL;
                    }

                    bool bReadyRightSwap = false;
                    while ((!bReadyRightSwap) && iL < iR)
                    {
                        Debug.Assert(iR >= iLIn && iR <= iRIn);
                        bReadyRightSwap = tmpVert[iR].Vert[channel] < fSep;
                        if (!bReadyRightSwap) --iR;
                    }

                    Debug.Assert((iL < iR) || !(bReadyLeftSwap && bReadyRightSwap));

                    if (bReadyLeftSwap && bReadyRightSwap)
                    {
                        var sTmp = tmpVert[iL];
                        Debug.Assert(iL < iR);
                        tmpVert[iL] = tmpVert[iR];
                        tmpVert[iR] = sTmp;
                        iL++;
                        iR--;
                    }
                }

                Debug.Assert(iL == (iR + 1) || (iL == iR));
                if (iL == iR)
                {
                    bool bReadyRightSwap = tmpVert[iR].Vert[channel] < fSep;
                    if (bReadyRightSwap)
                    {
                        iL++;
                    }
                    else
                    {
                        iR--;
                    }
                }

                // only need to weld when there is more than 1 instance of the (x,y,z)

                if (iLIn < iR)
                {
                    // weld all left of fSep
                    MergeVertsFast(triList, tmpVert, iLIn, iR);
                }

                if (iL < iRIn)
                {
                    // weld all right of (or equal to) fSep
                    MergeVertsFast(triList, tmpVert, iL, iRIn);
                }
            }
        }

        static void DegenPrologue(TriInfo[] triInfos, int[] triList, int nTriangles, int totalTris)
        {
            // locate quads with only one good triangle
            int t = 0;
            while (t < (totalTris - 1))
            {
                int iFO_a = triInfos[t + 0].OrgFaceNumber;
                int iFO_b = triInfos[t + 1].OrgFaceNumber;
                if (iFO_a == iFO_b) // this is a quad
                {
                    int bIsDeg_a = (triInfos[t + 0].Flag & MARK_DEGENERATE) != 0 ? 1 : 0;
                    int bIsDeg_b = (triInfos[t + 1].Flag & MARK_DEGENERATE) != 0 ? 1 : 0;
                    if ((bIsDeg_a ^ bIsDeg_b) != 0)
                    {
                        triInfos[t + 0].Flag |= QUAD_ONE_DEGEN_TRI;
                        triInfos[t + 1].Flag |= QUAD_ONE_DEGEN_TRI;
                    }
                    t += 2;
                }
                else
                {
                    t++;
                }
            }

            // reorder list so all degen triangles are moved to the back
            // without reordering the good triangles
            t = 0;
            bool bStillFindingGoodOnes = true;
            int iNextGoodTriangleSearchIndex = 1;
            while (t < nTriangles && bStillFindingGoodOnes)
            {
                bool bIsGood = (triInfos[t].Flag & MARK_DEGENERATE) == 0;
                if (bIsGood)
                {
                    if (iNextGoodTriangleSearchIndex < (t + 2))
                    {
                        iNextGoodTriangleSearchIndex = t + 2;
                    }
                }
                else
                {
                    // search for the first good triangle.
                    bool bJustADegenerate = true;
                    while (bJustADegenerate && iNextGoodTriangleSearchIndex < totalTris)
                    {
                        bIsGood = (triInfos[iNextGoodTriangleSearchIndex].Flag & MARK_DEGENERATE) == 0;
                        if (bIsGood) bJustADegenerate = false;
                        else iNextGoodTriangleSearchIndex++;
                    }

                    int t0 = t;
                    int t1 = iNextGoodTriangleSearchIndex;
                    iNextGoodTriangleSearchIndex++;
                    Debug.Assert(iNextGoodTriangleSearchIndex > (t + 1));

                    // swap triangle t0 and t1
                    if (!bJustADegenerate)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            (triList[(t1 * 3) + i], triList[(t0 * 3) + i]) = (triList[(t0 * 3) + i], triList[(t1 * 3) + i]);
                        }

                        (triInfos[t1], triInfos[t0]) = (triInfos[t0], triInfos[t1]);
                    }
                    else
                    {
                        // this is not supposed to happen
                        bStillFindingGoodOnes = false;
                    }
                }

                if (bStillFindingGoodOnes) t++;
            }

            Debug.Assert(bStillFindingGoodOnes);  // code will still work.
            Debug.Assert(nTriangles == t);
        }
        void InitTriInfo(TriInfo[] triInfos, int[] triList, int nTriangles)
        {
            // triInfos[f].Flag is cleared in GenerateInitialVerticesIndexList() which is called before this function.

            // generate neighbor info list
            for (int f = 0; f < nTriangles; f++)
            {
                for (int i = 0; i < 3; i++)
                {
                    triInfos[f].FaceNeighbors[i] = -1;
                    triInfos[f].AssignedGroup[i] = null;

                    triInfos[f].Os.X = 0.0f;
                    triInfos[f].Os.Y = 0.0f;
                    triInfos[f].Os.Z = 0.0f;

                    triInfos[f].Ot.X = 0.0f;
                    triInfos[f].Ot.Y = 0.0f;
                    triInfos[f].Ot.Z = 0.0f;

                    triInfos[f].MagS = 0;
                    triInfos[f].MagT = 0;

                    // assumed bad
                    triInfos[f].Flag |= GROUP_WITH_ANY;
                }
            }

            // evaluate first order derivatives
            for (int f = 0; f < nTriangles; f++)
            {
                // initial values
                var v1 = GetPosition(triList[(f * 3) + 0]);
                var v2 = GetPosition(triList[(f * 3) + 1]);
                var v3 = GetPosition(triList[(f * 3) + 2]);
                var t1 = GetTexCoord(triList[(f * 3) + 0]);
                var t2 = GetTexCoord(triList[(f * 3) + 1]);
                var t3 = GetTexCoord(triList[(f * 3) + 2]);

                float t21x = t2.X - t1.X;
                float t21y = t2.Y - t1.Y;
                float t31x = t3.X - t1.X;
                float t31y = t3.Y - t1.Y;
                var d1 = Vector3.Subtract(v2, v1);
                var d2 = Vector3.Subtract(v3, v1);

                float fSignedAreaSTx2 = t21x * t31y - t21y * t31x;
                var vOs = Vector3.Subtract(Vector3.Multiply(t31y, d1), Vector3.Multiply(t21y, d2)); // eq 18
                var vOt = Vector3.Add(Vector3.Multiply(-t31x, d1), Vector3.Multiply(t21x, d2)); // eq 19

                triInfos[f].Flag |= fSignedAreaSTx2 > 0 ? ORIENT_PRESERVING : 0;

                if (!Utils.NotZero(fSignedAreaSTx2)) continue;

                float fAbsArea = MathF.Abs(fSignedAreaSTx2);
                float fLenOs = vOs.Length();
                float fLenOt = vOt.Length();
                float fS = (triInfos[f].Flag & ORIENT_PRESERVING) == 0 ? -1f : 1f;
                if (Utils.NotZero(fLenOs)) triInfos[f].Os = Vector3.Multiply(fS / fLenOs, vOs);
                if (Utils.NotZero(fLenOt)) triInfos[f].Ot = Vector3.Multiply(fS / fLenOt, vOt);

                // evaluate magnitudes prior to normalization of vOs and vOt
                triInfos[f].MagS = fLenOs / fAbsArea;
                triInfos[f].MagT = fLenOt / fAbsArea;

                // if this is a good triangle
                if (Utils.NotZero(triInfos[f].MagS) && Utils.NotZero(triInfos[f].MagT))
                {
                    triInfos[f].Flag &= ~GROUP_WITH_ANY;
                }
            }

            // force otherwise healthy quads to a fixed orientation
            int t = 0;
            while (t < (nTriangles - 1))
            {
                int iFO_a = triInfos[t + 0].OrgFaceNumber;
                int iFO_b = triInfos[t + 1].OrgFaceNumber;
                if (iFO_a != iFO_b)
                {
                    t++;
                    continue;
                }

                // this is a quad
                bool bIsDegA = (triInfos[t + 0].Flag & MARK_DEGENERATE) != 0;
                bool bIsDegB = (triInfos[t + 1].Flag & MARK_DEGENERATE) != 0;

                // bad triangles should already have been removed by
                // DegenPrologue(), but just in case check bIsDeg_a and bIsDeg_a are false
                if ((bIsDegA || bIsDegB) == false)
                {
                    bool bOrientA = (triInfos[t + 0].Flag & ORIENT_PRESERVING) != 0;
                    bool bOrientB = (triInfos[t + 1].Flag & ORIENT_PRESERVING) != 0;

                    // if this happens the quad has extremely bad mapping!!
                    if (bOrientA != bOrientB)
                    {
                        //printf("found quad with bad mapping\n");
                        bool bChooseOrientFirstTri = false;
                        if ((triInfos[t + 1].Flag & GROUP_WITH_ANY) != 0)
                        {
                            bChooseOrientFirstTri = true;
                        }
                        else if (CalcTexArea(triList, (t + 0) * 3) >= CalcTexArea(triList, (t + 1) * 3))
                        {
                            bChooseOrientFirstTri = true;
                        }

                        // force match
                        int t0 = bChooseOrientFirstTri ? t : (t + 1);
                        int t1 = bChooseOrientFirstTri ? (t + 1) : t;
                        triInfos[t1].Flag &= ~ORIENT_PRESERVING;    // clear first
                        triInfos[t1].Flag |= triInfos[t0].Flag & ORIENT_PRESERVING;   // copy bit
                    }
                }
                t += 2;
            }

            // match up edge pairs
            BuildNeighborsFast(triInfos, triList, nTriangles);
        }
        static void BuildNeighborsFast(TriInfo[] triInfos, int[] triList, int nTriangles)
        {
            uint uSeed = INTERNAL_RND_SORT_SEED; // could replace with a random seed?

            // build array of edges
            Edge[] edges = new Edge[nTriangles * 3];
            for (int f = 0; f < nTriangles; f++)
            {
                int t = f * 3;
                for (int i = 0; i < 3; i++)
                {
                    int i0 = triList[t + i];
                    int i1 = triList[t + (i < 2 ? (i + 1) : 0)];
                    edges[t + i].I0 = i0 < i1 ? i0 : i1;           // put minimum index in i0
                    edges[t + i].I1 = !(i0 < i1) ? i0 : i1;        // put maximum index in i1
                    edges[t + i].F = f;                            // record face number
                }
            }

            // sort over all edges by i0, this is the pricy one.
            QuickSortEdges(edges, 0, nTriangles * 3 - 1, 0, uSeed); // sort channel 0 which is i0

            // sub sort over i1, should be fast.
            // could replace this with a 64 bit int sort over (i0,i1)
            // with i0 as msb in the quicksort call above.
            int iEntries = nTriangles * 3;
            int iCurStartIndex = 0;
            for (int i = 1; i < iEntries; i++)
            {
                if (edges[iCurStartIndex].I0 != edges[i].I0)
                {
                    int iL = iCurStartIndex;
                    int iR = i - 1;
                    iCurStartIndex = i;
                    QuickSortEdges(edges, iL, iR, 1, uSeed); // sort channel 1 which is i1
                }
            }

            // sub sort over f, which should be fast.
            // this step is to remain compliant with BuildNeighborsSlow() when
            // more than 2 triangles use the same edge (such as a butterfly topology).
            iCurStartIndex = 0;
            for (int i = 1; i < iEntries; i++)
            {
                if (edges[iCurStartIndex].I0 != edges[i].I0 || edges[iCurStartIndex].I1 != edges[i].I1)
                {
                    int iL = iCurStartIndex;
                    int iR = i - 1;
                    iCurStartIndex = i;
                    QuickSortEdges(edges, iL, iR, 2, uSeed); // sort channel 2 which is f
                }
            }

            // pair up, adjacent triangles
            for (int i = 0; i < iEntries; i++)
            {
                int i0 = edges[i].I0;
                int i1 = edges[i].I1;
                int f = edges[i].F;
                GetEdge(i0, i1, triList, f * 3, out int i0A, out int i1A, out int edgeNumA); // resolve index ordering and edge_num

                bool unassignedA = triInfos[f].FaceNeighbors[edgeNumA] == -1;
                if (!unassignedA) continue;

                // get true index ordering
                bool found = false;
                int edgeNumB = 0; // 0,1 or 2
                int j = i + 1;
                while (j < iEntries && i0 == edges[j].I0 && i1 == edges[j].I1 && !found)
                {
                    int t0 = edges[j].I0;
                    int t1 = edges[j].I1;
                    int t = edges[j].F;
                    GetEdge(t0, t1, triList, t * 3, out int i1B, out int i0B, out edgeNumB); // resolve index ordering and edge_num

                    // flip i0B and i1B
                    bool unassignedB = triInfos[t].FaceNeighbors[edgeNumB] == -1;
                    if (unassignedB && i0A == i0B && i1A == i1B)
                    {
                        found = true;
                    }
                    else
                    {
                        j++;
                    }
                }

                if (found)
                {
                    int t = edges[j].F;
                    triInfos[f].FaceNeighbors[edgeNumA] = t;
                    triInfos[t].FaceNeighbors[edgeNumB] = f;
                }
            }
        }
        static void QuickSortEdges(Edge[] sortBuffer, int iLeft, int iRight, int channel, uint uSeed)
        {
            // early out
            int iElems = iRight - iLeft + 1;
            if (iElems < 2)
            {
                return;
            }
            else if (iElems == 2)
            {
                if (sortBuffer[iLeft][channel] > sortBuffer[iRight][channel])
                {
                    (sortBuffer[iRight], sortBuffer[iLeft]) = (sortBuffer[iLeft], sortBuffer[iRight]);
                }
                return;
            }

            // Random
            uSeed = Randomize(uSeed);
            // Random end

            int iL = iLeft;
            int iR = iRight;
            int n = iR - iL + 1;
            Debug.Assert(n >= 0);
            int index = (int)(uSeed % (uint)n);

            int iMid = sortBuffer[index + iL][channel];

            do
            {
                while (sortBuffer[iL][channel] < iMid)
                {
                    iL++;
                }
                while (sortBuffer[iR][channel] > iMid)
                {
                    iR--;
                }

                if (iL <= iR)
                {
                    (sortBuffer[iR], sortBuffer[iL]) = (sortBuffer[iL], sortBuffer[iR]);
                    iL++;
                    iR--;
                }
            }
            while (iL <= iR);

            if (iLeft < iR)
            {
                QuickSortEdges(sortBuffer, iLeft, iR, channel, uSeed);
            }

            if (iL < iRight)
            {
                QuickSortEdges(sortBuffer, iL, iRight, channel, uSeed);
            }
        }

        static int Build4RuleGroups(TriInfo[] triInfos, Group[] groups, int[] triList, int nTriangles)
        {
            int iNrMaxGroups = nTriangles * 3;
            int iNrActiveGroups = 0;
            int iOffset = 0;
            for (int f = 0; f < nTriangles; f++)
            {
                for (int i = 0; i < 3; i++)
                {
                    // if not assigned to a group
                    if ((triInfos[f].Flag & GROUP_WITH_ANY) == 0 && triInfos[f].AssignedGroup[i] == null)
                    {
                        int vert_index = triList[(f * 3) + i];
                        Debug.Assert(iNrActiveGroups < iNrMaxGroups);
                        triInfos[f].AssignedGroup[i] = groups[iNrActiveGroups];
                        triInfos[f].AssignedGroup[i].VertexRepresentitive = vert_index;
                        triInfos[f].AssignedGroup[i].OrientPreservering = (triInfos[f].Flag & ORIENT_PRESERVING) != 0;
                        triInfos[f].AssignedGroup[i].FaceIndicesOffset = iOffset;
                        iNrActiveGroups++;

                        triInfos[f].AssignedGroup[i].AddTriToGroup(f);
                        bool bOrPre = (triInfos[f].Flag & ORIENT_PRESERVING) != 0;
                        int neigh_indexL = triInfos[f].FaceNeighbors[i];
                        int neigh_indexR = triInfos[f].FaceNeighbors[i > 0 ? (i - 1) : 2];

                        if (neigh_indexL >= 0) // neighbor
                        {
                            bool bAnswer = AssignRecur(triList, triInfos, neigh_indexL, triInfos[f].AssignedGroup[i]);

                            bool bOrPre2 = (triInfos[neigh_indexL].Flag & ORIENT_PRESERVING) != 0;
                            bool bDiff = bOrPre != bOrPre2;
                            Debug.Assert(bAnswer || bDiff);
                        }

                        if (neigh_indexR >= 0) // neighbor
                        {
                            bool bAnswer = AssignRecur(triList, triInfos, neigh_indexR, triInfos[f].AssignedGroup[i]);

                            bool bOrPre2 = (triInfos[neigh_indexR].Flag & ORIENT_PRESERVING) != 0;
                            bool bDiff = bOrPre != bOrPre2;
                            Debug.Assert(bAnswer || bDiff);
                        }

                        // update offset
                        iOffset += triInfos[f].AssignedGroup[i].NFaces;
                        // since the groups are disjoint a triangle can never
                        // belong to more than 3 groups. Subsequently something
                        // is completely screwed if this assertion ever hits.
                        Debug.Assert(iOffset <= iNrMaxGroups);
                    }
                }
            }

            return iNrActiveGroups;
        }
        static bool AssignRecur(int[] triList, TriInfo[] triInfos, int myTriIndex, Group group)
        {
            ref var myTriInfo = ref triInfos[myTriIndex];

            // track down vertex
            int iVertRep = group.VertexRepresentitive;
            int[] pVerts = [triList[(3 * myTriIndex) + 0], triList[(3 * myTriIndex) + 1], triList[(3 * myTriIndex) + 2]];
            int i = -1;
            if (pVerts[0] == iVertRep) i = 0;
            else if (pVerts[1] == iVertRep) i = 1;
            else if (pVerts[2] == iVertRep) i = 2;
            Debug.Assert(i >= 0 && i < 3);

            // early out
            if (myTriInfo.AssignedGroup[i] == group)
            {
                return true;
            }
            else if (myTriInfo.AssignedGroup[i] != null)
            {
                return false;
            }

            if ((myTriInfo.Flag & GROUP_WITH_ANY) != 0)
            {
                // first to group with a group-with-anything triangle
                // determines it's orientation.
                // This is the only existing order dependency in the code!!
                if (myTriInfo.AssignedGroup[0] == null &&
                    myTriInfo.AssignedGroup[1] == null &&
                    myTriInfo.AssignedGroup[2] == null)
                {
                    myTriInfo.Flag &= ~ORIENT_PRESERVING;
                    myTriInfo.Flag |= group.OrientPreservering ? ORIENT_PRESERVING : 0;
                }
            }

            bool bOrient = (myTriInfo.Flag & ORIENT_PRESERVING) != 0;
            if (bOrient != group.OrientPreservering)
            {
                return false;
            }

            group.AddTriToGroup(myTriIndex);
            myTriInfo.AssignedGroup[i] = group;

            int neigh_indexL = myTriInfo.FaceNeighbors[i];
            int neigh_indexR = myTriInfo.FaceNeighbors[i > 0 ? (i - 1) : 2];
            if (neigh_indexL >= 0)
            {
                AssignRecur(triList, triInfos, neigh_indexL, group);
            }
            if (neigh_indexR >= 0)
            {
                AssignRecur(triList, triInfos, neigh_indexR, group);
            }

            return true;
        }

        bool GenerateTSpaces(TSpace[] tspace, TriInfo[] triInfos, Group[] groups, int nActiveGroups, int[] triList, float thresCos)
        {
            int iMaxNrFaces = 0;
            for (int g = 0; g < nActiveGroups; g++)
            {
                if (iMaxNrFaces < groups[g].NFaces)
                {
                    iMaxNrFaces = groups[g].NFaces;
                }
            }
            if (iMaxNrFaces == 0) return true;

            // make initial allocations
            TSpace[] pSubGroupTspace = new TSpace[iMaxNrFaces];
            SubGroup[] pUniSubGroups = new SubGroup[iMaxNrFaces];
            int[] pTmpMembers = new int[iMaxNrFaces];

            int iUniqueTspaces = 0;
            for (int g = 0; g < nActiveGroups; g++)
            {
                var pGroup = groups[g];
                int iUniqueSubGroups = 0;

                for (int i = 0; i < pGroup.NFaces; i++)  // triangles
                {
                    int f = pGroup.GetFaceIndex(i);  // triangle number
                    int index = -1;
                    if (triInfos[f].AssignedGroup[0] == pGroup) index = 0;
                    else if (triInfos[f].AssignedGroup[1] == pGroup) index = 1;
                    else if (triInfos[f].AssignedGroup[2] == pGroup) index = 2;
                    Debug.Assert(index >= 0 && index < 3);

                    int iVertIndex = triList[(f * 3) + index];
                    Debug.Assert(iVertIndex == pGroup.VertexRepresentitive);

                    // is normalized already
                    var n = GetNormal(iVertIndex);

                    // project
                    var vOs = Vector3.Subtract(triInfos[f].Os, Vector3.Multiply(Vector3.Dot(n, triInfos[f].Os), n));
                    var vOt = Vector3.Subtract(triInfos[f].Ot, Vector3.Multiply(Vector3.Dot(n, triInfos[f].Ot), n));
                    if (Utils.NotZero(vOs)) vOs = Vector3.Normalize(vOs);
                    if (Utils.NotZero(vOt)) vOt = Vector3.Normalize(vOt);

                    // original face number
                    int iOF_1 = triInfos[f].OrgFaceNumber;

                    int iMembers = 0;
                    for (int j = 0; j < pGroup.NFaces; j++)
                    {
                        int t = pGroup.GetFaceIndex(j);  // triangle number
                        int iOF_2 = triInfos[t].OrgFaceNumber;

                        // project
                        var vOs2 = Vector3.Subtract(triInfos[t].Os, Vector3.Multiply(Vector3.Dot(n, triInfos[t].Os), n));
                        var vOt2 = Vector3.Subtract(triInfos[t].Ot, Vector3.Multiply(Vector3.Dot(n, triInfos[t].Ot), n));
                        if (Utils.NotZero(vOs2)) vOs2 = Vector3.Normalize(vOs2);
                        if (Utils.NotZero(vOt2)) vOt2 = Vector3.Normalize(vOt2);

                        bool bAny = ((triInfos[f].Flag | triInfos[t].Flag) & GROUP_WITH_ANY) != 0;
                        // make sure triangles which belong to the same quad are joined.
                        bool bSameOrgFace = iOF_1 == iOF_2;

                        float fCosS = Vector3.Dot(vOs, vOs2);
                        float fCosT = Vector3.Dot(vOt, vOt2);

                        Debug.Assert(f != t || bSameOrgFace); // sanity check
                        if (bAny || bSameOrgFace || (fCosS > thresCos && fCosT > thresCos))
                        {
                            pTmpMembers[iMembers++] = t;
                        }
                    }

                    // sort pTmpMembers
                    SubGroup tmp_group;
                    tmp_group.NFaces = iMembers;
                    tmp_group.TriMembers = pTmpMembers;
                    if (iMembers > 1)
                    {
                        uint uSeed = INTERNAL_RND_SORT_SEED;    // could replace with a random seed?
                        QuickSort(pTmpMembers, 0, iMembers - 1, uSeed);
                    }

                    // look for an existing match
                    bool bFound = false;
                    int l = 0;
                    while (l < iUniqueSubGroups && !bFound)
                    {
                        bFound = CompareSubGroups(tmp_group, pUniSubGroups[l]);
                        if (!bFound) ++l;
                    }

                    // assign tangent space index
                    Debug.Assert(bFound || l == iUniqueSubGroups);
                    //piTempTangIndices[f*3+index] = iUniqueTspaces+l;

                    // if no match was found we allocate a new subgroup
                    if (!bFound)
                    {
                        // insert new subgroup
                        pUniSubGroups[iUniqueSubGroups].NFaces = iMembers;
                        pUniSubGroups[iUniqueSubGroups].TriMembers = tmp_group.TriMembers;
                        pSubGroupTspace[iUniqueSubGroups] = EvalTspace(tmp_group.TriMembers, iMembers, triList, triInfos, pGroup.VertexRepresentitive);
                        iUniqueSubGroups++;
                    }

                    // output tspace
                    int iOffs = triInfos[f].TSpacesOffs;
                    int iVert = triInfos[f].VertNum[index];
                    Debug.Assert(tspace[iOffs + iVert].Counter < 2);
                    Debug.Assert((triInfos[f].Flag & ORIENT_PRESERVING) != 0 == pGroup.OrientPreservering);
                    if (tspace[iOffs + iVert].Counter == 1)
                    {
                        tspace[iOffs + iVert] = AvgTSpace(tspace[iOffs + iVert], pSubGroupTspace[l]);
                        tspace[iOffs + iVert].Counter = 2;  // update counter
                        tspace[iOffs + iVert].Orient = pGroup.OrientPreservering;
                    }
                    else
                    {
                        Debug.Assert(tspace[iOffs + iVert].Counter == 0);
                        tspace[iOffs + iVert] = pSubGroupTspace[l];
                        tspace[iOffs + iVert].Counter = 1;  // update counter
                        tspace[iOffs + iVert].Orient = pGroup.OrientPreservering;
                    }
                }

                // clean up and offset iUniqueTspaces
                for (int s = 0; s < iUniqueSubGroups; s++)
                {
                    pUniSubGroups[s].TriMembers = null;
                }
                iUniqueTspaces += iUniqueSubGroups;
            }

            return true;
        }
        TSpace EvalTspace(int[] faceIndices, int faces, int[] triList, TriInfo[] triInfos, int iVertexRepresentitive)
        {
            TSpace res = new();
            res.Os.X = 0.0f;
            res.Os.Y = 0.0f;
            res.Os.Z = 0.0f;
            res.Ot.X = 0.0f;
            res.Ot.Y = 0.0f;
            res.Ot.Z = 0.0f;
            res.MagS = 0;
            res.MagT = 0;

            float fAngleSum = 0;
            for (int face = 0; face < faces; face++)
            {
                int f = faceIndices[face];

                // only valid triangles get to add their contribution
                if ((triInfos[f].Flag & GROUP_WITH_ANY) != 0)
                {
                    continue;
                }

                int i = -1;
                if (triList[(3 * f) + 0] == iVertexRepresentitive) i = 0;
                else if (triList[(3 * f) + 1] == iVertexRepresentitive) i = 1;
                else if (triList[(3 * f) + 2] == iVertexRepresentitive) i = 2;
                Debug.Assert(i >= 0 && i < 3);

                // project
                int index = triList[(3 * f) + i];
                Vector3 n = GetNormal(index);
                Vector3 vOs = Vector3.Subtract(triInfos[f].Os, Vector3.Multiply(Vector3.Dot(n, triInfos[f].Os), n));
                Vector3 vOt = Vector3.Subtract(triInfos[f].Ot, Vector3.Multiply(Vector3.Dot(n, triInfos[f].Ot), n));
                if (Utils.NotZero(vOs)) vOs = Vector3.Normalize(vOs);
                if (Utils.NotZero(vOt)) vOt = Vector3.Normalize(vOt);

                int i2 = triList[(3 * f) + (i < 2 ? (i + 1) : 0)];
                int i1 = triList[(3 * f) + i];
                int i0 = triList[(3 * f) + (i > 0 ? (i - 1) : 2)];

                var p0 = GetPosition(i0);
                var p1 = GetPosition(i1);
                var p2 = GetPosition(i2);
                var v1 = Vector3.Subtract(p0, p1);
                var v2 = Vector3.Subtract(p2, p1);

                // project
                v1 = Vector3.Subtract(v1, Vector3.Multiply(Vector3.Dot(n, v1), n)); if (Utils.NotZero(v1)) v1 = Vector3.Normalize(v1);
                v2 = Vector3.Subtract(v2, Vector3.Multiply(Vector3.Dot(n, v2), n)); if (Utils.NotZero(v2)) v2 = Vector3.Normalize(v2);

                // weight contribution by the angle
                // between the two edge vectors
                float fCos = Vector3.Dot(v1, v2);
                fCos = fCos > 1 ? 1 : (fCos < (-1) ? (-1) : fCos);
                float fAngle = MathF.Acos(fCos);
                float fMagS = triInfos[f].MagS;
                float fMagT = triInfos[f].MagT;

                res.Os = Vector3.Add(res.Os, Vector3.Multiply(fAngle, vOs));
                res.Ot = Vector3.Add(res.Ot, Vector3.Multiply(fAngle, vOt));
                res.MagS += fAngle * fMagS;
                res.MagT += fAngle * fMagT;
                fAngleSum += fAngle;
            }

            // normalize
            if (Utils.NotZero(res.Os)) res.Os = Vector3.Normalize(res.Os);
            if (Utils.NotZero(res.Ot)) res.Ot = Vector3.Normalize(res.Ot);
            if (fAngleSum > 0)
            {
                res.MagS /= fAngleSum;
                res.MagT /= fAngleSum;
            }

            return res;
        }
        static TSpace AvgTSpace(TSpace pTS0, TSpace pTS1)
        {
            TSpace ts_res = new();

            // this if is important. Due to floating point precision
            // averaging when ts0==ts1 will cause a slight difference
            // which results in tangent space splits later on
            if (pTS0.MagS == pTS1.MagS && pTS0.MagT == pTS1.MagT && pTS0.Os == pTS1.Os && pTS0.Ot == pTS1.Ot)
            {
                ts_res.MagS = pTS0.MagS;
                ts_res.MagT = pTS0.MagT;
                ts_res.Os = pTS0.Os;
                ts_res.Ot = pTS0.Ot;
            }
            else
            {
                ts_res.MagS = 0.5f * (pTS0.MagS + pTS1.MagS);
                ts_res.MagT = 0.5f * (pTS0.MagT + pTS1.MagT);
                ts_res.Os = Vector3.Add(pTS0.Os, pTS1.Os);
                ts_res.Ot = Vector3.Add(pTS0.Ot, pTS1.Ot);
                if (Utils.NotZero(ts_res.Os)) ts_res.Os = Vector3.Normalize(ts_res.Os);
                if (Utils.NotZero(ts_res.Ot)) ts_res.Ot = Vector3.Normalize(ts_res.Ot);
            }

            return ts_res;
        }
        static void QuickSort(int[] sortBuffer, int iLeft, int iRight, uint uSeed)
        {
            // Random
            uSeed = Randomize(uSeed);
            // Random end

            int iL = iLeft;
            int iR = iRight;
            int n = iR - iL + 1;
            Debug.Assert(n >= 0);
            int index = (int)(uSeed % (uint)n);

            int iMid = sortBuffer[index + iL];
            do
            {
                while (sortBuffer[iL] < iMid)
                {
                    iL++;
                }
                while (sortBuffer[iR] > iMid)
                {
                    iR--;
                }

                if (iL <= iR)
                {
                    (sortBuffer[iR], sortBuffer[iL]) = (sortBuffer[iL], sortBuffer[iR]);
                    iL++;
                    iR--;
                }
            }
            while (iL <= iR);

            if (iLeft < iR)
            {
                QuickSort(sortBuffer, iLeft, iR, uSeed);
            }
            if (iL < iRight)
            {
                QuickSort(sortBuffer, iL, iRight, uSeed);
            }
        }
        static bool CompareSubGroups(SubGroup pg1, SubGroup pg2)
        {
            if (pg1.NFaces != pg2.NFaces)
            {
                return false;
            }

            int i = 0;
            bool bStillSame = true;
            while (i < pg1.NFaces && bStillSame)
            {
                bStillSame = pg1.TriMembers[i] == pg2.TriMembers[i];
                if (bStillSame) i++;
            }

            return bStillSame;
        }

        void DegenEpilogue(TSpace[] tspace, TriInfo[] triInfos, int[] triList, int nTriangles, int totalTris)
        {
            // deal with degenerate triangles
            // punishment for degenerate triangles is O(N^2)
            for (int t = nTriangles; t < totalTris; t++)
            {
                // degenerate triangles on a quad with one good triangle are skipped
                // here but processed in the next loop
                bool bSkip = (triInfos[t].Flag & QUAD_ONE_DEGEN_TRI) != 0;
                if (bSkip) continue;

                for (int i = 0; i < 3; i++)
                {
                    int index1 = triList[(t * 3) + i];

                    // search through the good triangles
                    bool bNotFound = true;
                    int j = 0;
                    while (bNotFound && j < (3 * nTriangles))
                    {
                        int index2 = triList[j];
                        if (index1 == index2)
                        {
                            bNotFound = false;
                        }
                        else
                        {
                            j++;
                        }
                    }

                    if (!bNotFound)
                    {
                        int iTri = j / 3;
                        int iVert = j % 3;
                        int iSrcVert = triInfos[iTri].VertNum[iVert];
                        int iSrcOffs = triInfos[iTri].TSpacesOffs;
                        int iDstVert = triInfos[t].VertNum[i];
                        int iDstOffs = triInfos[t].TSpacesOffs;

                        // copy tspace
                        tspace[iDstOffs + iDstVert] = tspace[iSrcOffs + iSrcVert];
                    }
                }
            }

            // deal with degenerate quads with one good triangle
            for (int t = 0; t < nTriangles; t++)
            {
                // this triangle belongs to a quad where the
                // other triangle is degenerate
                if ((triInfos[t].Flag & QUAD_ONE_DEGEN_TRI) == 0) continue;

                var pV = triInfos[t].VertNum;
                int iFlag = (1 << pV[0]) | (1 << pV[1]) | (1 << pV[2]);
                int iMissingIndex = 0;
                if ((iFlag & 2) == 0) iMissingIndex = 1;
                else if ((iFlag & 4) == 0) iMissingIndex = 2;
                else if ((iFlag & 8) == 0) iMissingIndex = 3;

                int iOrgF = triInfos[t].OrgFaceNumber;
                var vDstP = GetPosition(MakeIndex(iOrgF, iMissingIndex));
                bool bNotFound = true;
                int i = 0;
                while (bNotFound && i < 3)
                {
                    int iVert = pV[i];
                    var vSrcP = GetPosition(MakeIndex(iOrgF, iVert));
                    if (vSrcP == vDstP)
                    {
                        int iOffs = triInfos[t].TSpacesOffs;
                        tspace[iOffs + iMissingIndex] = tspace[iOffs + iVert];
                        bNotFound = false;
                    }
                    else
                    {
                        i++;
                    }
                }
                Debug.Assert(!bNotFound);
            }
        }

        float CalcTexArea(int[] triList, int idx)
        {
            var t1 = GetTexCoord(triList[idx + 0]);
            var t2 = GetTexCoord(triList[idx + 1]);
            var t3 = GetTexCoord(triList[idx + 2]);

            float t21x = t2.X - t1.X;
            float t21y = t2.Y - t1.Y;
            float t31x = t3.X - t1.X;
            float t31y = t3.Y - t1.Y;

            float fSignedAreaSTx2 = t21x * t31y - t21y * t31x;

            return fSignedAreaSTx2 < 0f ? -fSignedAreaSTx2 : fSignedAreaSTx2;
        }
        static void GetEdge(int i0In, int i1In, int[] triList, int idx, out int i0, out int i1, out int edgeNum)
        {
            // test if first index is on the edge
            if (triList[idx + 0] == i0In || triList[idx + 0] == i1In)
            {
                // test if second index is on the edge
                if (triList[idx + 1] == i0In || triList[idx + 1] == i1In)
                {
                    edgeNum = 0; // first edge
                    i0 = triList[idx + 0];
                    i1 = triList[idx + 1];
                }
                else
                {
                    edgeNum = 2; // third edge
                    i0 = triList[idx + 2];
                    i1 = triList[idx + 0];
                }
            }
            else
            {
                // only second and third index is on the edge
                edgeNum = 1; // second edge
                i0 = triList[idx + 1];
                i1 = triList[idx + 2];
            }
        }

        static int FindGridCell(float fMin, float fMax, float fVal)
        {
            float fIndex = MAX_CELLS * ((fVal - fMin) / (fMax - fMin));
            int iIndex = (int)fIndex;
            return iIndex < MAX_CELLS ? (iIndex >= 0 ? iIndex : 0) : (MAX_CELLS - 1);
        }
        static void IndexToData(int indexIn, out int face, out int vert)
        {
            vert = indexIn & 0x3;
            face = indexIn >> 2;
        }
        static int MakeIndex(int face, int vert)
        {
            Debug.Assert(vert >= 0 && vert < 4 && face >= 0);
            return (face << 2) | (vert & 0x3);
        }
    }
}
