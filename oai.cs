using System;
using System.Reflection;

namespace oai2
{
	///<author>
	///Terry Reese
    ///The Ohio State University Libraries
	///Oregon State University Libraries
	///</author>
	///
	///<email>
	///reeset@gmail.com
	///</email>
	///
	///<summary>
	///This class represents the first OAI harvester, to my knowledge, written
	///in C#, designed specifically to work within Microsoft's .NET or MONO's .NET
	///frameworks.  The class was inspired by Ed Summer's OAI::Harvester module for
	///PERL. 
	/// </summary>
	/// 
	/// <modified>
	/// ===========================================================================
	/// September 24, 2004:
	/// Initial Release
	/// ===========================================================================
	/// ===========================================================================
	/// October 16, 2004
	/// Change the way that the Web Request is made.  Many thanks to Lucien van Wouw
	/// for letting me know that DSpace IRs require that the web request include
	/// a user-agent.  No other OAI repository made this requirement -- but to keep
	/// problems from arising, I've modified the code so that a user-agent is 
	/// passed.  The default user-agent is now: "OSU .NET OAI Harvester" -- however
	/// this is stored as a constant that you may feel free to change.
	/// ============================================================================
	/// ============================================================================
	/// October 17, 2004
	/// Modified ListRecords, ListSets and ListMetadataFormat so that the harvester
	/// will work with OAI repositories that export data in a single line.  
	/// Apparently the xmltextreader would read beyond the node if there wasn't 
	/// white space.  This has been corrected.  To make sure that I account for 
	/// this later, I've added an example to the example program.
	/// Also -- added a count property to the ListRecord class so that its
	/// easier to enumerate.
	/// =============================================================================
	/// November 5, 2004
	/// Changes by Frank McCown - Old Dominion University
	/// 1) Allowed access to request URL when accessing OAI repository.
	/// 2) Allowed access to raw XML response.
	/// 3) Fixed access with resumption token in ListRecords().
	/// 4) Added new ListRecords constructor.
	/// 5) Made DC metadata extraction more robust.
	/// 6) Spead up XML parsing in some contexts.
	/// =============================================================================
	/// 
	/// ===============================================================================
	/// October November 4, 2005
	/// Updated the resumptionToken tag.  It wasn't escaping the necessary characters.
	/// This is an issue that shows up in how NSF structures their resumption tokens.
	/// ===============================================================================
	/// </modified>
	/// 
	/// <license>
	/// This project has been released under the GNU General Public License.  See: http://www.gnu.org/licenses/gpl.txt
	/// </license>
	public class OAI
	{		
		private string cUserAgent = "MarcEdit 7 (OAI Harvester)";
		private string pbaseURL;
		private string prequestURL;
		private string prawXML;
		private string presponseDate;
		private int pTimeout = 150000;
		public Error error = new Error();

        private bool IsNumeric(string s) 
		{
			if (s.Length==0) return false;
			for (int x=0; x<s.Length; x++) 
			{
				if (Char.IsNumber(s[x])==false)
				{
					return false;
				}
			}
			return true;
		}

		private string baseURL 
		{
			set 
			{
				pbaseURL=value;
			}
			get 
			{
				return pbaseURL;
			}
		}

		public int Timeout 
		{
			set 
			{
				pTimeout = value;
			} get 
			  {
				  return pTimeout;
			  }
		}

        public string UserAgent
        {
            set
            {
                cUserAgent = value;
            }
            get
            {
                return cUserAgent;
            }
        }

		public string RequestURL
		{
			set
			{
				prequestURL = value;
			}
			get
			{
				return prequestURL;
			}
		}

		public string RawXML
		{
			set
			{
				prawXML = value;
			}
			get
			{
				return prawXML;
			}
		}

		public string ResponseDate
		{
			set
			{
				presponseDate = value;
			}
			get
			{
				return presponseDate;
			}
		}


		public OAI(string sURI)
		{
			//
			// TODO: Add constructor logic here
			//
			baseURL = sURI;
		}
		
		//=======================================================================
		// Sub/Function: identify
		// Description: Use this function to return the identifier information 
		// from an OAI repository.
		//
		// example: 
		// OAI objOAI = new OAI("http://memory.loc.gov/cgi-bin/oai2_0");
		// Identify objID = new Identify();
		//
		// objID = objOAI.identify();
		// Console.WriteLine("Base URL: " + objID.baseURL);
		// Console.WriteLine("Repository: " + objID.repositoryName);
		// Console.WriteLine("Deleted Records: "  + objID.deletedRecord);
		//=======================================================================

		public Identify identify() 
		{
			System.IO.Stream objStream;
			Identify objID = new Identify();
			System.Net.HttpWebRequest wr;
			System.Xml.XmlTextReader rd;

			try 
			{
				prequestURL = baseURL + "?verb=Identify";
				wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(prequestURL);
				wr.UserAgent = cUserAgent;
		
				System.Net.WebResponse response = wr.GetResponse();
				objStream = response.GetResponseStream();
				System.IO.StreamReader reader = new System.IO.StreamReader(objStream);
				prawXML = reader.ReadToEnd();
				reader.Close();
				rd = new System.Xml.XmlTextReader(prawXML, System.Xml.XmlNodeType.Document, null);	
			}
			catch (Exception e)
			{
                
				error.errorName = e.ToString();
				error.errorDescription = e.Message + "\n<br>Unable to connect to " + baseURL; 
				return null;
			}
			
			while (rd.Read()) 
			{
				if (rd.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (rd.Name == "responseDate")
					{
						presponseDate = rd.ReadString();
					}
                    else if (rd.Name=="Identify") 
					{
						while ( rd.Read() && rd.NodeType != System.Xml.XmlNodeType.EndElement) 
						{
							switch (rd.Name) 
							{
								case "repositoryName":
									objID.repositoryName = ParseOAI(ref rd, "repositoryName");
									break;
								case "baseURL":
									objID.baseURL = ParseOAI(ref rd, "baseURL");
									break;
								case "protocolVersion":
									objID.protocolVersion = ParseOAI(ref rd, "protocolVersion");
									break;
								case "earliestDatestamp":
									objID.earliestDatestamp = ParseOAI(ref rd, "earliestDatestamp");
									break;
								case "deletedRecord":
									objID.deletedRecord = ParseOAI (ref rd, "deletedRecord");
									break;
								case "granularity":
									objID.granularity = ParseOAI(ref rd, "granularity");
									break;
								case "adminEmail":
									objID.adminEmail.Add(ParseOAI(ref rd, "adminEmail"));
									break;
								case "compression":
									objID.compression.Add(ParseOAI(ref rd, "compression"));
									break;
								case "description":
									objID.description.Add(ParseOAIContainer(ref rd, "description"));
									break;
							}
						} 
					} 
					else if (rd.Name=="error") 
					{
						error.errorName = rd.GetAttribute("code");
						error.errorDescription = rd.ReadString();
						return null;
					}
				}
			}

			rd.Close();

			return objID;
		}


		/*=======================================================================
		// Sub/Function: listMetadataFormats
		// Description: Use this function to return the metadataformats available
		// for a specific repository or set.
		// 
		// example: 
		// OAI objOAI = new OAI("http://memory.loc.gov/cgi-bin/oai2_0");
		// ListMetadataFormats objFormat = new ListMetadataFormats();
		// objFormat = objOAI.listMetadataFormats();
		//
		//	Console.WriteLine("Prefix: " + String.Join(",", (string[]) objFormat.metadataPrefix.ToArray(typeof(string))));
		//	Console.WriteLine("Schema: " + String.Join(",", (string[]) objFormat.schema.ToArray(typeof(string))));
		//	Console.WriteLine("Namespace: " + String.Join(",", (string[]) objFormat.metadataNamespace.ToArray(typeof(string))));
		//======================================================================*/

		public ListMetadataFormats listMetadataFormats() 
		{
			return listMetadataFormats("");
		}

		public ListMetadataFormats listMetadataFormats(string sidentifier) 
		{
			System.IO.Stream objStream;
			ListMetadataFormats objFormat = new ListMetadataFormats();
			System.Net.HttpWebRequest wr;
			System.Xml.XmlTextReader rd;
			
			if (sidentifier.Length==0) 
			{
				prequestURL = baseURL + "?verb=ListMetadataFormats";
			} 
			else 
			{
				prequestURL = baseURL + "?verb=ListMetadataFormats&identifier=" + sidentifier;
			}

			try 
			{
				wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(prequestURL);
				wr.UserAgent = cUserAgent;
				System.Net.WebResponse response = wr.GetResponse();
				objStream = response.GetResponseStream();
				System.IO.StreamReader reader = new System.IO.StreamReader(objStream);
				prawXML = reader.ReadToEnd();
				reader.Close();
				rd = new System.Xml.XmlTextReader(prawXML, System.Xml.XmlNodeType.Document, null);
			} 
			catch (Exception e)
			{
                
				error.errorName = e.ToString();
				error.errorDescription = e.Message + "\n<br>Unable to connect to " + baseURL; 
				return null;
			}
			
			while (rd.Read()) 
			{
				if (rd.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (rd.Name == "responseDate")
					{
						presponseDate = rd.ReadString();
					}
					else if (rd.Name=="ListMetadataFormats") 
					{
						while (rd.Read())  // && rd.NodeType != System.Xml.XmlNodeType.EndElement) 
						{			
							switch (rd.Name) 
							{
								case "metadataPrefix":
									objFormat.metadataPrefix.Add(ParseOAI(ref rd, "metadataPrefix"));
									break;
								case "schema":
									objFormat.schema.Add(ParseOAI(ref rd, "schema"));
									break;
								case "metadataNamespace":
									objFormat.metadataNamespace.Add(ParseOAI(ref rd, "metadataNamespace"));
									break;
								default:
									break;
							}
						} 
					} 
					else if (rd.Name=="error") 
					{
						error.errorName = rd.GetAttribute("code");
						error.errorDescription = rd.ReadString();
						return null;
					}
				}
			}

			rd.Close();
			return objFormat;
		}


		//========================================================================
		// Sub/Function: GetRecord
		// Description: GetRecord returns the individual metadata object for a 
		// specific item.  
		// Parameters: 
		//	identifier [required]: the identifier of the item.
		//  sPrefix [option]: Specifies the metadata prefix to use.
		//	By default, dublin core is supported but if you wrote your
		//	own handler, you could use other schemes with this class.
		// example:
		// objFormat = new ListMetadataFormats();
		// OAI objOSU = new OAI("http://digitalcollections.library.oregonstate.edu/cgi-bin/oai.exe");
		// objFormat = objOSU.listMetadataFormats("oai:digitalcollections.library.oregonstate.edu:bracero/37");
		//
		// Console.WriteLine("Prefix: " + String.Join(",", (string[]) objFormat.metadataPrefix.ToArray(typeof(string))));
		// Console.WriteLine("Schema: " + String.Join(",", (string[]) objFormat.schema.ToArray(typeof(string))));
		// Console.WriteLine("Namespace: " + String.Join(",", (string[]) objFormat.metadataNamespace.ToArray(typeof(string))));
		//
		// Console.WriteLine();
		// 
		// Record objRecord = new Record();
		// objRecord = objOSU.GetRecord("oai:digitalcollections.library.oregonstate.edu:bracero/37");
		// Console.WriteLine("Header Information:");
		// Console.WriteLine(objRecord.header.identifier);
		// Console.WriteLine(objRecord.header.datestamp);
		// Console.WriteLine();
		// 
		// Console.WriteLine("Record Info:");
		// Console.WriteLine("Title: " + String.Join(",", (string[]) objRecord.metadata.title.ToArray(typeof(string))));
		// Console.WriteLine("Description: " + String.Join(",", (string[]) objRecord.metadata.description.ToArray(typeof(string))));
		//
		// Console.WriteLine();
		//
		//=========================================================================
		public Record GetRecord(string sidentifier) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return GetRecord(sidentifier, "oai_dc", ref objHandler);
		}

		public Record GetRecord(string sidentifier, string sPrefix) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return GetRecord(sidentifier, sPrefix, ref objHandler);
		}

		public Record GetRecord(string sidentifier, string sPrefix, ref Object objHandler) 
		{
			System.IO.Stream objStream;
			Record objRecord;
			string tmp = "";
			System.Net.HttpWebRequest wr;
			System.Xml.XmlTextReader rd;

			if (sPrefix.Length==0) 
			{
				sPrefix = "oai_dc";
			}

			prequestURL = baseURL + "?verb=GetRecord&metadataPrefix=" + sPrefix + "&identifier=" + sidentifier;

			//======================================================
			// If you wanted to support additional metadata formats, 
			// you would just need to have additional handlers.
			//======================================================

			try 
			{

                


                wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(prequestURL);
				wr.UserAgent = cUserAgent;
                wr.Accept = "application/xml;*/*";

                if (this.Timeout <= 0) { this.Timeout = 100000; }
                wr.Timeout = this.Timeout;


                System.Net.WebResponse response = wr.GetResponse();
				objStream = response.GetResponseStream();
				System.IO.StreamReader reader = new System.IO.StreamReader(objStream);
				prawXML = reader.ReadToEnd();
				reader.Close();
				rd = new System.Xml.XmlTextReader(prawXML, System.Xml.XmlNodeType.Document, null);
                rd.Normalization = true;
			}
			catch (Exception e)
			{
                
				error.errorName = e.ToString();
				error.errorDescription = e.Message + "\n<br>Unable to connect to " + baseURL; 
				return null;
			}
			
			while (rd.Read()) 
			{
				if (rd.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (rd.Name == "responseDate")
					{
						presponseDate = rd.ReadString();
					}
					else if (rd.Name=="GetRecord") 
					{ 
						do
						{
							if (rd.Name=="record") 
							{
								tmp = ParseOAIContainer(ref rd, "record");
								objRecord = new Record(tmp, ref objHandler);
								return objRecord;
							}
							else rd.Read(); // Added the Read() and will never occur with the ReadInnerXml()

						} while (rd.Name!="GetRecord"); // loop
					} 
					else if (rd.Name=="error") 
					{
						error.errorName = rd.GetAttribute("code");
						error.errorDescription = rd.ReadString();
						return null;
					}
				}
			}

			return null;

		}


		//===========================================================================
		// Sub/Function: ListRecords
		// Description: Retrieves a set or list of records from an OAI repository.
		// supports the resumptionToken
		//============================================================================
		public ListRecord ListRecords(string sPrefix) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return ListRecords(sPrefix, "", "", "", null, ref objHandler);
		}
		public ListRecord ListRecords(string sPrefix, string sset) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return ListRecords(sPrefix, sset, "", "", null, ref objHandler);
		}
		public ListRecord ListRecords(string sPrefix, string sset, string sfrom, string suntil) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return ListRecords(sPrefix, sset, sfrom, suntil, null, ref objHandler);
		}
		public ListRecord ListRecords(string sPrefix, string sfrom, string suntil) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return ListRecords(sPrefix, "", sfrom, suntil, null, ref objHandler);
		}
		public ListRecord ListRecords(ResumptionToken objToken) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return ListRecords("", "", "", "", objToken, ref objHandler);
		}
		// McCown
		public ListRecord ListRecords(string sPrefix, string sset, string sfrom, string suntil, ResumptionToken objToken) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return ListRecords(sPrefix, sset, sfrom, suntil, objToken, ref objHandler);
		}

		public ListRecord ListRecords(string sPrefix, 
			string sset, 
			string sfrom, 
			string suntil, 
			ResumptionToken objToken, 
			ref Object objHandler) 
		{
            int fail = 0;
            Restart:
			System.IO.Stream objStream;
			ListRecord objList = new ListRecord();
			Record objRecord;
			ResumptionToken myToken;
			string tmp = "";
			System.Net.HttpWebRequest wr = null;
            System.Xml.XmlTextReader rd = null;

			if (sPrefix.Length==0) 
			{
				sPrefix = "oai_dc";
			}

			if (objToken==null) 
			{
				if (sset.Length!=0) 
				{
					sset = "&set=" + sset;
				} 
				if (sfrom.Length!=0) 
				{
					sfrom = "&from=" + sfrom;
				}
				if (suntil.Length!=0) 
				{
					suntil = "&until=" + suntil;
				}

				prequestURL = baseURL + "?verb=ListRecords&metadataPrefix=" + sPrefix + sset + sfrom + suntil;

			} 
			else 
			{
				//This is where we handle the resumptionToken - McCown
				prequestURL = baseURL + "?verb=ListRecords&resumptionToken=" + objToken.resumptionToken;
                //System.Windows.Forms.MessageBox.Show(prequestURL);
			}
			//======================================================
			// If you wanted to support additional metadata formats, 
			// you would just need to have additional handlers.
			//======================================================
            try
            {
                //System.Windows.Forms.MessageBox.Show(prequestURL);
                wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(prequestURL);
                //wr.Headers.Add("Host:" + System.Net.Dns.GetHostName());
                wr.UserAgent = cUserAgent; // + DateTime.Now.ToString();
                wr.Accept = "application/xml;*/*";

                if (this.Timeout <= 0) { this.Timeout = 100000; }
                wr.Timeout = this.Timeout;

                System.Net.WebResponse response = wr.GetResponse();
                objStream = response.GetResponseStream();
                System.IO.StreamReader reader = new System.IO.StreamReader(objStream);
                prawXML = reader.ReadToEnd();
                //System.Windows.Forms.MessageBox.Show(prawXML);
                reader.Close();
                response.Close();
                rd = new System.Xml.XmlTextReader(prawXML, System.Xml.XmlNodeType.Document, null);
                rd.Normalization = true;
            }
            catch (System.Net.WebException ee)
            {
                int sleepval = 3000;
                
                fail++;
                //System.Windows.Forms.MessageBox.Show(tt.ToString());
                //System.Windows.Forms.MessageBox.Show(ee.ToString());
                if (ee.Status == System.Net.WebExceptionStatus.ProtocolError)
                {
                    var response = ee.Response as System.Net.HttpWebResponse;
                    if (response != null)
                    {
                        if ((int)response.StatusCode == 503)
                        {
                            string retryafter = response.Headers["Retry-After"];
                            if (retryafter != null && IsNumeric(retryafter) == true)
                            {
                                {
                                    sleepval = System.Convert.ToInt32(retryafter) * 1000;
                                }
                            }
                        }
                    }

                    if (fail <= 4)
                    {
                        //System.Windows.Forms.MessageBox.Show(sleepval.ToString());
                        System.Threading.Thread.Sleep(sleepval);
                        goto Restart;
                    }
                    else
                    {
                        wr.Abort();
                        error.errorName = ee.ToString();
                        error.errorDescription = ee.Message + "\n<br>Unable to connect to " + baseURL;
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
               // System.Windows.Forms.MessageBox.Show("2");
                if (wr != null)
                {
                    wr.Abort();
                }
                error.errorName = e.ToString();
                error.errorDescription = e.Message + "\n<br>Unable to connect to " + baseURL;
                return null;
            }

            fail = 0;
			while (rd.Read()) 
			{
				if (rd.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (rd.Name == "responseDate")
					{
						presponseDate = rd.ReadString();
					}
					else if (rd.Name=="ListRecords") 
					{ 
						do 
						{
                            
							if (rd.Name=="record") 
							{
								tmp = ParseOAIContainer(ref rd, "record");
								objRecord = new Record(tmp,ref objHandler);
								objList.record.Add(objRecord);
								//return objRecord;
							} 
							else if (rd.Name=="resumptionToken") 
							{
								tmp=rd.ReadOuterXml();
								myToken = new ResumptionToken(tmp);
								objList.token = myToken;
							}
                            //else if (rd.Name.ToLower() == "OAI-PMH".ToLower())
                            //{
                            //    break;
                            //}
                            else if (rd.EOF == true)
                            {
                                error.errorName = "Empty ListRecords Request";
                                error.errorDescription = "No data was returned in the ListRecords Element.  This is likely an error.";
                                return null;
                            }
                            else rd.Read(); // Added the Read() and will never occur with the ReadInnerXml()

						} while (rd.Name!="ListRecords"); // loop
					} 
					else if (rd.Name=="error") 
					{
						error.errorName = rd.GetAttribute("code");
						error.errorDescription = rd.ReadString();
						return null;
					}
				}
			}

            
            return objList;
            

			
		}


		//============================================================
		// Sub/Function: ListIdentifiers
		// Description: Like ListRecords, ListIdentifiers returns 
		// just the record identifiers for a set of records from an 
		// oai repository.
		//=============================================================
		public ListIdentifier  ListIdenifiers(string sPrefix) 
		{
			return ListIdenifiers(sPrefix, "", "", "", null);
		}
		public ListIdentifier ListIdenifiers(string sPrefix, string sset) 
		{
			return ListIdenifiers(sPrefix, sset, "", "", null);
		}
		public ListIdentifier ListIdenifiers(string sPrefix, string sset, string sfrom, string suntil) 
		{
			return ListIdenifiers(sPrefix, sset, sfrom, suntil, null);
		}
		public ListIdentifier ListIdenifiers(string sPrefix, string sfrom, string suntil) 
		{
			return ListIdenifiers(sPrefix, "", sfrom, suntil, null);
		}
		public ListIdentifier ListIdenifiers(ResumptionToken objToken) 
		{
			return ListIdenifiers("", "", "", "", objToken);
		}
		public ListIdentifier ListIdenifiers(string sPrefix, 
			string sset, 
			string sfrom, 
			string suntil, 
			ResumptionToken objToken) 
		{
			System.IO.Stream objStream;
			ListIdentifier objList = new ListIdentifier();
			Identifiers objRecord;
			ResumptionToken myToken;
			string tmp = "";
			System.Net.HttpWebRequest wr;
			System.Xml.XmlTextReader rd;

			if (sPrefix.Length==0) 
			{
				sPrefix = "oai_dc";
			}

			if (objToken==null) 
			{
				if (sset.Length!=0) 
				{
					sset = "&set=" + sset;
				} 
				if (sfrom.Length!=0) 
				{
					sfrom = "&from=" + sfrom;
				}
				if (suntil.Length!=0) 
				{
					suntil = "&until=" + suntil;
				}

				prequestURL = baseURL + "?verb=ListIdentifiers&metadataPrefix=" + sPrefix + sset + sfrom + suntil;

			} 
			else 
			{
				prequestURL = baseURL + "?verb=ListIdentifiers&resumptionToken=" + objToken.resumptionToken;
				//This is where we handle the resumptionToken
			}
			//======================================================
			// If you wanted to support additional metadata formats, 
			// you would just need to have additional handlers.
			//======================================================
			//Console.Write(sURL);
			try 
			{
				wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(prequestURL);
				wr.UserAgent = cUserAgent;
				System.Net.WebResponse response = wr.GetResponse();
				objStream = response.GetResponseStream();
				System.IO.StreamReader reader = new System.IO.StreamReader(objStream);
				prawXML = reader.ReadToEnd();
				reader.Close();
				rd = new System.Xml.XmlTextReader(prawXML, System.Xml.XmlNodeType.Document, null);
			}
			catch (Exception e)
			{
               
				error.errorName = "badConnection";
				error.errorDescription = e.Message + "<br>Unable to connect to " + baseURL;
				return null;
			}
			
			while (rd.Read()) 
			{
				if (rd.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (rd.Name == "responseDate")
					{
						presponseDate = rd.ReadString();
					}
					else if (rd.Name=="ListIdentifiers") 
					{ 
						do
						{
							if (rd.Name=="header") 
							{
								tmp = rd.ReadOuterXml();
								//tmp += ParseOAIContainer(ref rd, "header", true);
								//Console.WriteLine("In the Function: " + tmp);
								objRecord = new Identifiers(tmp);
								objList.record.Add(objRecord);
								//return objRecord;
							} 
							else if (rd.Name=="resumptionToken") 
							{
								tmp=rd.ReadOuterXml();
								myToken = new ResumptionToken(tmp);
								objList.token = myToken;
							}
							else rd.Read(); // Added the Read() and will never occur with the ReadInnerXml()

						} while (rd.Name!="ListIdentifiers"); // loop
					} 
					else if (rd.Name=="error") 
					{
						error.errorName = rd.GetAttribute("code");
						error.errorDescription = rd.ReadString();
						rd.Close();
						return null;
					}
				}
			}

			rd.Close();
			return objList;			
		}

		//=========================================================
		// Sub/Function: ListSets
		// Description: Returns a list of collections currently 
		// available for harvesting from this oai server.
		//==========================================================
		public ListSet ListSets(ResumptionToken objToken) 
		{
			object objHandler = new object();
			objHandler = new OAI_DC();
			return ListSets(objToken, ref objHandler);
		}

		public ListSet ListSets(ResumptionToken objToken, ref Object objHandler) 
		{
			System.IO.Stream objStream;
			OAI_LIST objRecord;
			ListSet objList = new ListSet();
			ResumptionToken myToken;
			string tmp = "";
			System.Net.HttpWebRequest wr;
			System.Xml.XmlTextReader rd;
			
			if (objToken==null) 
			{			
				prequestURL = baseURL + "?verb=ListSets";
			} 
			else 
			{
				prequestURL = baseURL + "?verb=ListSets&resumptionToken=" + objToken.resumptionToken;
				//This is where we handle the resumptionToken
			}
			//======================================================
			// If you wanted to support additional metadata formats, 
			// you would just need to have additional handlers.
			//======================================================

			try 
			{
				wr = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(prequestURL);
				wr.UserAgent = cUserAgent;
				System.Net.WebResponse response = wr.GetResponse();
				objStream = response.GetResponseStream();
				System.IO.StreamReader reader = new System.IO.StreamReader(objStream);
				prawXML = reader.ReadToEnd();
				reader.Close();
				rd = new System.Xml.XmlTextReader(prawXML, System.Xml.XmlNodeType.Document, null);			
			}
			catch (Exception e)
			{
				error.errorName = e.ToString();
				error.errorDescription = e.Message + "\n<br>Unable to connect to " + baseURL; 
				return null;
			}
			
			while (rd.Read()) 
			{
				if (rd.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (rd.Name == "responseDate")
					{
						presponseDate = rd.ReadString();
					}
					else if (rd.Name=="ListSets") 
					{ 
						//while (rd.Read()) 
						do
						{
							if (rd.Name=="set") 
							{
								objRecord = new OAI_LIST(rd.ReadInnerXml(), ref objHandler);
								objList.listset.Add(objRecord);
								//return objRecord;
							} 
							else if (rd.Name=="resumptionToken") 
							{
								tmp=rd.ReadOuterXml();
								myToken = new ResumptionToken(tmp);
								objList.token = myToken;
							}
							else rd.Read(); // Added the Read() and will never occur with the ReadInnerXml()

					   } while (rd.Name!="ListSets"); // loop
					} 
					else if (rd.Name == "error") 
					{
						error.errorName = rd.GetAttribute("code");
						error.errorDescription = rd.ReadString();
						return null;
					}
				}
			}

			rd.Close();
			return objList;			
		}


		//================================================================================
		// Function/Sub: ParseOAIcontainer
		// Description: Function used to return an entire XML node
		//================================================================================
		private string ParseOAIContainer(ref System.Xml.XmlTextReader reader, string sNode) 
		{
			
			while (reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (reader.Name == sNode) 
				{
					return reader.ReadInnerXml();		
				}
			}
			return "";
		}

		//================================================================================
		// Function/Sub: ParseOAI
		// Description: Function used to return a single value from a named element.
		//================================================================================
		private string ParseOAI(ref System.Xml.XmlTextReader reader, string sNode) 
		{
			while (reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (reader.Name == sNode) 
				{
					return reader.ReadString();
				}
			}
			return "";
		}

	}

	public class Identify 
	{

		private string prepositoryName="";
		private string pbaseURL = "";
		private string pprotocolVersion = "";
		private string pearliestDatestamp = "";
		private string pdeletedRecord = "";
		private string pgranularity = "";
		private System.Collections.ArrayList padminEmail = new System.Collections.ArrayList();
		private System.Collections.ArrayList pcompression = new System.Collections.ArrayList();
		private System.Collections.ArrayList pdescription = new System.Collections.ArrayList();


		public string repositoryName 
		{
			set 
			{
				prepositoryName = value;
			} get 
			  {
				  return prepositoryName;
			  }
		}

		public string baseURL 
		{
			set 
			{
				pbaseURL = value;
			} get 
			  {
				  return pbaseURL;
			  }
		}

		public string protocolVersion 
		{
			set 
			{
				pprotocolVersion= value;
			} get 
			  {
				  return pprotocolVersion;
			  }
		}

		public string earliestDatestamp 
		{
			set 
			{
				pearliestDatestamp = value;
			} get 
			  {
				  return pearliestDatestamp;
			  }
		}

		public string deletedRecord 
		{
			set 
			{
				pdeletedRecord = value;
			} get 
			  {
				  return pdeletedRecord;
			  }
		}

		public string granularity 
		{
			set 
			{
				pgranularity = value;
			} get 
			  {
				  return pgranularity;
			  }
		}

		public System.Collections.ArrayList adminEmail 
		{
			set 
			{
				padminEmail.Add(value);
			} get 
			  {
				  return padminEmail;
			  }
		}

		public System.Collections.ArrayList compression 
		{
			set 
			{
				pcompression.Add(value);
			} get 
			  {
				  return pcompression;
			  }
		}

		public System.Collections.ArrayList description 
		{
			set 
			{
				pdescription.Add(value);
			} get 
			  {
				  return pdescription;
			  }
		}
	}

	public class ListMetadataFormats 
	{
	
		private System.Collections.ArrayList pmetadataPrefix = new System.Collections.ArrayList();
		private System.Collections.ArrayList pschema = new System.Collections.ArrayList();
		private System.Collections.ArrayList pmetadataNamespace = new System.Collections.ArrayList();

	
		public System.Collections.ArrayList metadataPrefix 
		{
			set 
			{
				pmetadataPrefix.Add(value);
			} get 
			  {
				  return pmetadataPrefix;
			  }
		}

		public System.Collections.ArrayList schema 
		{
			set 
			{
				pschema.Add(value);
			} get 
			  {
				  return pschema;
			  }
		}

		public System.Collections.ArrayList metadataNamespace 
		{
			set 
			{
				pmetadataNamespace.Add(value);
			} get 
			  {
				  return pmetadataNamespace;
			  }
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public class Record 
	{
		public Object metadata;
		public OAI_Header header;
		public OAI_About about;
		private string pmetadata = "";

		public string RecordXML 
		{
			set 
			{
				pmetadata = value;
			} get 
			  {
				  return pmetadata;
			  }
		}

		public Record() 
		{
			//just initialize 
		}
		public Record(string sXML, ref Object objHandler) 
		{
			//Object localHandler = new objHandler();

			RecordXML = sXML;
			header = new OAI_Header(sXML);
			Type theHandlerType = Type.GetType(objHandler.ToString());
			Object[] tobject = new object[1];
			tobject[0] = sXML;

			metadata =  theHandlerType.InvokeMember("", BindingFlags.DeclaredOnly | 
            BindingFlags.Public | BindingFlags.NonPublic | 
            BindingFlags.Instance | BindingFlags.CreateInstance, null,  null, tobject);
			about = new OAI_About(sXML, ref objHandler);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class Identifiers 
	{
		public OAI_Header header;


		public Identifiers() 
		{
			//just initialize 
		}
		public Identifiers(string sXML) 
		{
			header = new OAI_Header(sXML);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class OAI_LIST  
	{
		public string setSpec = "";
		public string setName = "";
		public Object description;

		public OAI_LIST() 
		{
			//initial
		}

		public OAI_LIST(string sXML, ref Object objHandler) 
		{
			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(sXML, System.Xml.XmlNodeType.Element, null);
			while (reader.Read()) 
			{
				if (reader.Name == "setSpec") 
				{
					setSpec = reader.ReadString();
				} 
				else if (reader.Name=="setName") 
				{
					setName = reader.ReadString();
				} 
				else if (reader.Name=="setDescription") 
				{
					Type theHandlerType = Type.GetType(objHandler.ToString());
					Object[] tobject = new object[1];
					tobject[0] = reader.ReadInnerXml();

					description =  theHandlerType.InvokeMember("", BindingFlags.DeclaredOnly | 
						BindingFlags.Public | BindingFlags.NonPublic | 
						BindingFlags.Instance | BindingFlags.CreateInstance, null,  null, tobject);
				}
			}

		}

	}

	public class OAI_DC
	{
		private System.Collections.ArrayList ptitle = new System.Collections.ArrayList();
		private System.Collections.ArrayList pcreator = new System.Collections.ArrayList();
		private System.Collections.ArrayList psubject = new System.Collections.ArrayList();
		private System.Collections.ArrayList pdescription = new System.Collections.ArrayList();
		private System.Collections.ArrayList ppublisher = new System.Collections.ArrayList();
		private System.Collections.ArrayList pcontributor = new System.Collections.ArrayList();
		private System.Collections.ArrayList pdate = new System.Collections.ArrayList();
		private System.Collections.ArrayList ptype = new System.Collections.ArrayList();
		private System.Collections.ArrayList pformat = new System.Collections.ArrayList();
		private System.Collections.ArrayList pidentifier = new System.Collections.ArrayList();
		private System.Collections.ArrayList psource = new System.Collections.ArrayList();
		private System.Collections.ArrayList planguage = new System.Collections.ArrayList();
		private System.Collections.ArrayList prelation = new System.Collections.ArrayList();
		private System.Collections.ArrayList pcoverage = new System.Collections.ArrayList();
		private System.Collections.ArrayList prights = new System.Collections.ArrayList();
		
		public System.Collections.ArrayList title 
		{
			set 
			{
				ptitle.Add(value);
			} get 
			  {
				  return ptitle;
			  }
		}

		public System.Collections.ArrayList creator 
		{
			set 
			{
				pcreator.Add(value);
			} get 
			  {
				  return pcreator;
			  }
		}

		public System.Collections.ArrayList subject 
		{
			set 
			{
				psubject.Add(value);
			} get 
			  {
				  return psubject;
			  }
		}

		public System.Collections.ArrayList description 
		{
			set 
			{
				pdescription.Add(value);
			} get 
			  {
				  return pdescription;
			  }
		}

		public System.Collections.ArrayList publisher 
		{
			set 
			{
				ppublisher.Add(value);
			} get 
			  {
				  return ppublisher;
			  }
		}

		public System.Collections.ArrayList contributor 
		{
			set 
			{
				pcontributor.Add(value);
			} get 
			  {
				  return pcontributor;
			  }
		}

		public System.Collections.ArrayList date 
		{
			set 
			{
				pdate.Add(value);
			} get 
			  {
				  return pdate;
			  }
		}

		public System.Collections.ArrayList type 
		{
			set 
			{
				ptype.Add(value);
			} get 
			  {
				  return ptype;
			  }
		}

		public System.Collections.ArrayList format 
		{
			set 
			{
				pformat.Add(value);
			} get 
			  {
				  return pformat;
			  }
		}

		public System.Collections.ArrayList identifier 
		{
			set 
			{
				pidentifier.Add(value);
			} get 
			  {
				  return pidentifier;
			  }
		}

		public System.Collections.ArrayList source 
		{
			set 
			{
				psource.Add(value);
			} get 
			  {
				  return psource;
			  }
		}

		public System.Collections.ArrayList language 
		{
			set 
			{
				planguage.Add(value);
			} get 
			  {
				  return planguage;
			  }
		}

		public System.Collections.ArrayList relation 
		{
			set 
			{
				prelation.Add(value);
			} get 
			  {
				  return prelation;
			  }
		}

		public System.Collections.ArrayList coverage 
		{
			set 
			{
				pcoverage.Add(value);
			} get 
			  {
				  return pcoverage;
			  }
		}

		public System.Collections.ArrayList rights 
		{
			set 
			{
				prights.Add(value);
			} get 
			  {
				  return prights;
			  }
		}


		public OAI_DC() 
		{
			//Simply for initialization
		}

		public OAI_DC(string sXML) 
		{
			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(sXML, System.Xml.XmlNodeType.Element, null);
			while (reader.Read() && reader.Name != "oai_dc:dc" && reader.Name != "oaidc:dc");  // Keep reading until finding oia_dc
			
			string oiaDCxml = reader.ReadInnerXml();

			reader = new System.Xml.XmlTextReader(oiaDCxml, System.Xml.XmlNodeType.Element, null);

			while (reader.Read()) 
			{
				string metadata = reader.Name.Replace("dc:", "");  // Remove optional dc:
				switch (metadata) 
				{
					case "title":
						title.Add(reader.ReadString());
						break;
					case "creator":
						creator.Add(reader.ReadString());
						break;
					case "subject":
						subject.Add(reader.ReadString());
						break;
					case "description":
						description.Add(reader.ReadString());
						break;
					case "publisher":
						publisher.Add(reader.ReadString());
						break;
					case "contributor":
						contributor.Add(reader.ReadString());
						break;
					case "date":
						date.Add(reader.ReadString());
						break;
					case "type":
						type.Add(reader.ReadString());
						break;
					case "format":
						format.Add(reader.ReadString());
						break;
					case "identifier":
						identifier.Add(reader.ReadString());
						break;
					case "source":
						source.Add(reader.ReadString());
						break;
					case "language":
						language.Add(reader.ReadString());
						break;
					case "relation":
						relation.Add(reader.ReadString());
						break;
					case "coverage":
						coverage.Add(reader.ReadString());
						break;
					case "rights":
						rights.Add(reader.ReadString());
						break;
					default:
						break;
				}
			} // end while 
		}

	}  // end class


	/// <summary>
	/// Represents header of OAI record.
	/// </summary>
	public class OAI_Header 
	{
		private string pidentifier="";
		private string pdatestamp="";
		private System.Collections.ArrayList psetSpec = new System.Collections.ArrayList();
		private string pstatus= "";

		public string identifier 
		{
			set 
			{
				pidentifier = value;
			} get 
			  {
				  return pidentifier;
			  }
		}

		public string datestamp 
		{
			set 
			{
				pdatestamp = value;
			} get 
			  {
				  return pdatestamp;
			  }
		}

		public string status 
		{
			set 
			{
				pstatus = value;
			} get 
			  {
				  return pstatus;
			  }
		}
		
		public System.Collections.ArrayList setSpec 
		{
			set 
			{
				psetSpec.Add(value);
			} get 
			  {
				  return psetSpec;
			  }
		}

		public OAI_Header(string sXML) 
		{
			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(sXML, System.Xml.XmlNodeType.Element, null);
			while (reader.Read() && reader.Name != "header");  // Keep searching until finding header

			if (reader.Name == "header") 
			{
				status = reader.GetAttribute("status");

				while (reader.Read()) 
				{						
					switch (reader.Name) 
					{						
						case "identifier":
							if (identifier.Length == 0)   // In case this is the DC indentifier
								identifier = reader.ReadString();
							break;
						case "datestamp":
							datestamp = reader.ReadString();
							break;
						case "setSpec":
							setSpec.Add(reader.ReadString());
							break;
					}
				}
			}
						
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public class OAI_About 
	{
		public Object objDC;
		public OAI_About(string sXML, ref Object objHandler) 
		{
			Type theHandlerType = Type.GetType(objHandler.ToString());
			Object[] tobject = new object[1];
			tobject[0] = sXML;

			objDC =  theHandlerType.InvokeMember("", BindingFlags.DeclaredOnly | 
				BindingFlags.Public | BindingFlags.NonPublic | 
				BindingFlags.Instance | BindingFlags.CreateInstance, null,  null, tobject);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class ListRecord 
	{
		private System.Collections.ArrayList pRecord = new System.Collections.ArrayList();
		private ResumptionToken pToken = new ResumptionToken();

		private static int pindex=0;
		public System.Collections.ArrayList record 
		{
			set 
			{
				pRecord.Add(value);
			} get 
			  {
				  return pRecord;
			  }
		}

		public ResumptionToken token 
		{
			set 
			{ 
				pToken = value;
			} get 
			  {
				  return pToken;
			  }
		}

		public Record next() 
		{
			if (pindex <= (record.Count-1)) 
			{
				Record objRecord = (Record)record[pindex];
				pindex++;
				return objRecord;
			} 
			else 
			{
				return null;
			}
		}

		public Record previous() 
		{
			if (pindex >= 0) 
			{
				Record objRecord = (Record)record[pindex];
				if (pindex > 0) 
				{
					pindex--;
				}
				return objRecord;
			} 
			else 
			{
				return null;
			}
		}
		public int count() 
		{
			return record.Count;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class ListIdentifier 
	{
		private System.Collections.ArrayList pRecord = new System.Collections.ArrayList();
		private ResumptionToken pToken = null;  // new ResumptionToken();   McCown

		private static int pindex=0;
		public System.Collections.ArrayList record
		{
			set 
			{
				pRecord.Add(value);
			} get 
			  {
				  return pRecord;
			  }
		}

		public ResumptionToken token 
		{
			set 
			{
				pToken = value;
			} get 
			  {
				  return pToken;
			  }
		}
			
		public Record next() 
		{
			if (pindex <= (record.Count-1)) 
			{
				Record objRecord = (Record)record[pindex];
				pindex++;
				return objRecord;
			} 
			else 
			{
				return null;
			}
		}
		public Record previous() 
		{
			if (pindex >=0) 
			{
				Record objRecord = (Record)record[pindex];
				if (pindex > 0) 
				{
					pindex--;
				}
				return objRecord;
			} 
			else 
			{
				return null;
			}
		}
		public int count() 
		{
			return record.Count;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class ListSet 
	{
		private System.Collections.ArrayList pOAI_LIST = new System.Collections.ArrayList();
		private ResumptionToken pToken = null; //new ResumptionToken();  McCown
		//private static int pindex=0;
		public System.Collections.ArrayList listset 
		{
			set 
			{
				pOAI_LIST.Add(value);
			} get 
			  {
				  return pOAI_LIST;
			  }
		}
		public ResumptionToken token 
		{
			set 
			{
				pToken = value;
			} get 
			  {
				  return pToken;
			  }
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public class ResumptionToken
	{
		private string pexpirationDate=null;
		private string pcompleteListSize=null;
		private string pcursor=null;
		private string presumptionToken=null;
        private bool pisEscaped = false;

		internal string EscapeSpecial(string s) 
		{
			s = s.Replace("%", "%25");
			s = s.Replace("/", "%2F");
			s = s.Replace("?", "%3F");
			s = s.Replace("#", "%23");
			s = s.Replace("=", "%3D");
			s = s.Replace("&", "%26");
			s = s.Replace(":", "%3A");
			s = s.Replace(";", "%3B");
			s = s.Replace(" ", "%20");
			s = s.Replace("+", "%2B");
			return s;
		}

        public bool IsEscaped
        {
            set
            { pisEscaped = value;
            }
            get
            {
                return pisEscaped;
            }
        }
		public string expirationDate 
		{
			set 
			{
				pexpirationDate = value;
			} get 
			  {
				  return pexpirationDate;
			  }
		}

		public string completeListSize 
		{
			set 
			{
				pcompleteListSize = value;
			} get 
			  {
				  return pcompleteListSize;
			  }
		}

		public string cursor 
		{
			set 
			{
				pcursor = value;
			} get 
			  {
				  return pcursor;
			  }
		}

		public string resumptionToken 
		{
			set 
			{
                if (IsEscaped == false)
                {
                    presumptionToken = value;//EscapeSpecial(value);
                } else
                {
                    presumptionToken = EscapeSpecial(value);
                }
			} get 
			  {
				  return presumptionToken;
			  }
		}

		public ResumptionToken() 
		{
			//Just to initialize;
		}

		public ResumptionToken(string sXML) 
		{
			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(sXML, System.Xml.XmlNodeType.Element, null);
			while (reader.Read()) 
			{
				if (reader.Name == "resumptionToken") 
				{
                    try
                    {
                        expirationDate = reader.GetAttribute("expirationDate");
                    }
                    catch
                    {
                        expirationDate = null;
                    }

                    try
                    {
                        completeListSize = reader.GetAttribute("completeListSize");
                    }
                    catch
                    {
                        completeListSize = null;
                    }
                    try
                    {
                        cursor = reader.GetAttribute("cursor");
                    }
                    catch
                    {
                        cursor = null;
                    }

                    try
                    {
                        resumptionToken = reader.ReadString();
                    }
                    catch
                    {
                        resumptionToken = null;
                    }
				}
			}		
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class Error 
	{
		private string perrorName = null;
		private string perrorDescription = null;

		public string errorName 
		{
			set 
			{
				perrorName = value;
			} get 
			  {
				  return perrorName;
			  }
		}

		public string errorDescription 
		{
			set 
			{
				perrorDescription = value;
			} get 
			  {
				  return perrorDescription;
			  }
		}		
	}
}
