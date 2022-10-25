const e = require("express");
const { connect } = require("http2");

module.exports = class DatabaseController {
  constructor(mysql, fs, defaultDataDir, imageNotFoundFile, tmpDir) {
    this.mysql = mysql;
    this.fs = fs;
    this.defaultDataDir = defaultDataDir;
    this.imageNotFoundFile = imageNotFoundFile;
    this.tmpDir = tmpDir;
    this.asyncWaitDelay = 50;

    this.con = this.mysql.createConnection({
      host: "localhost",
      user: "root",
      password: "steelersrule",
      database: "constellations"
    });

    this.con.connect(function (err) {
      if (err) throw err;
      console.log("Connected to db!");
    });
  }

  randomString = (length) => {
    const chars = '0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ';
    var result = '';
    for (var i = length; i > 0; --i) result += chars[Math.floor(Math.random() * chars.length)];
    return result;
  }

  randomStringUppercase = (length) => {
    const chars = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    var result = '';
    for (var i = length; i > 0; --i) result += chars[Math.floor(Math.random() * chars.length)];
    return result;
  }

  cleanStr = (str) => {
    let ret = str;

    if(ret.length == 0){
      return "";
    }

    while(ret.charAt(ret.length - 1) == "/"){
      ret = ret.substring(0, ret.length - 1);
      if(ret.length == 0){
        return "";
      }
    }

    if(ret.length == 0){
      return "";
    }

    while(ret.charAt(0) == "/"){
      ret = ret.substring(1, ret.length);
      if(ret.length == 0){
        return "";
      }
    }

    return ret;
  }

  concatPaths = (paths) => {
    let retPath = this.cleanStr(this.defaultDataDir);
    for(let i = 0; i < paths.length; i++){
      retPath = retPath + "/" + this.cleanStr(paths[i]);
    }
    return this.cleanStr(retPath);
  }

  concatPathsTmp = (paths) => {
    let retPath = this.cleanStr(this.tmpDir);
    for(let i = 0; i < paths.length; i++){
      retPath = retPath + "/" + this.cleanStr(paths[i]);
    }
    return this.cleanStr(retPath);
  }

  accessGalaxyAndReadLinker = (connectCode, secretCode, callback) => {
    //callback = (wasError (int), constellations [], tmpId)

    this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(4, undefined, "");
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            let tmpId = results[0].identifier;

            this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
              if (err){
                callback(1, undefined, "");
                return;
              }

              let {constellations} = JSON.parse(data);
              callback(0, constellations, tmpId);

            });
          }
          else{
            callback(2, undefined, "");
          }
        }
        else{
          callback(3, undefined, "");
        }
      }
    });
  }

  galaxyExists = (connectCode, secretCode, callback) => {
    this.con.query('SELECT secretJoinCode FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(connectCode, secretCode, 0);
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            callback(connectCode, secretCode, 1);
          }
          else{
            callback(connectCode, secretCode, 2);
          }
        }
        else{
          callback(connectCode, secretCode, 0);
        }
      }

    });
  }

  createGalaxy = (name, code, creator, callback) => {
    let connectCode = this.randomStringUppercase(8);
    let tmpId = this.randomString(20);

    this.con.query('SELECT name FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results.length < 1){
        this.con.query('SELECT name FROM galaxies WHERE identifier=?', [tmpId], (error2, results2, fields2) => {

          if(results2.length < 1){
            this.con.query('INSERT INTO galaxies (name, secretJoinCode, creator, clientConnectCode, identifier) VALUES (?, ?, ?, ?, ?)', [name, code, creator, connectCode, tmpId], (error3, results3, fields3) => {
              if (error3){
                throw error3;
              }
              else{
                this.fs.mkdir(this.concatPaths([tmpId]), (err) => {});
                this.fs.mkdir(this.concatPaths([tmpId, "imageData"]), (err) => {});
                this.fs.mkdir(this.concatPaths([tmpId, "noteData"]), (err) => {});
                this.fs.mkdir(this.concatPaths([tmpId, "pointData"]), (err) => {});
                this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify({"constellations":[]}), (err) => {});

                callback(connectCode, code);
              }
            });
          }
          else{
            this.createGalaxy(name, code, creator);
          }

        });
      }
      else{
        this.createGalaxy(name, code, creator);
      }

    });

  }

  waitForAsyncCompletion = (completionStatus, toInt, delay, callback) =>{
    if(completionStatus.timesRan == toInt){
      callback();
    }
    else{
      setTimeout(() => {
        this.waitForAsyncCompletion(completionStatus, toInt, delay, callback);
      }, delay);
    }
  }

  buildGalaxy = (connectCode, secretCode, callback) => {
    //connectCode, secretCode, galaxyName, galaxyCreator, existInt, constellations

    this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(connectCode, secretCode, "", "", 0, []);
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            let tmpId = results[0].identifier;

            this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
              if (err){
                callback(connectCode, secretCode, "", "", 3, []);
                return;
              }

              let {constellations} = JSON.parse(data);
              let retConstellations = [];

              /*SYNC CODE
              for(let i = 0; i < constellations.length; i++){
                let rawdata = this.fs.readFileSync(this.concatPaths([tmpId, "pointData", constellations[i].pointData]));
                let {points} = JSON.parse(rawdata);
                retConstellations.push({"uid":constellations[i].uid, "name":constellations[i].name, "stars":points});
              }
              callback(connectCode, secretCode, results[0].name, results[0].creator, 1, retConstellations);
              // END SYNC CODE */

              //ASYNC FIX
              let completionStatus = {timesRan: 0};

              for(let i = 0; i < constellations.length; i++){
                this.fs.readFile(this.concatPaths([tmpId, "pointData", constellations[i].pointData]), (err, rawdata) => {
                  let {points} = JSON.parse(rawdata);
                  retConstellations.push({"uid":constellations[i].uid, "name":constellations[i].name, "stars":points});
                  completionStatus.timesRan++;
                });
              }

              this.waitForAsyncCompletion(completionStatus, constellations.length, this.asyncWaitDelay, () =>{
                callback(connectCode, secretCode, results[0].name, results[0].creator, 1, retConstellations);
              });
              //END ASYNC FIX

            });

          }
          else{
            callback(connectCode, secretCode, "", "", 2, []);
          }
        }
        else{
          callback(connectCode, secretCode, "", "", 0, []);
        }
      }

    });
  }

  createConstellation = (connectCode, secretCode, constellationName, points, callback) => {
    //connectCode, secretCode, existsInt, constellationUID, constellationName

    this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(connectCode, secretCode, 0, "", "");
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            let tmpId = results[0].identifier;

            this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
              if (err){
                callback(connectCode, secretCode, 3, "", "");
                return;
              }

              let {constellations} = JSON.parse(data);
              let newConstellationUID = this.randomString(20);
              let newConstellation = {"uid": newConstellationUID, "name":constellationName, "pointData": `_${newConstellationUID}.json`, "imageData":[], "noteData":[]};

              constellations.push(newConstellation);
              let newWritableConstellations = {"constellations":constellations};

              this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify(newWritableConstellations), err => {
                if(err){
                  callback(connectCode, secretCode, 4, "", "");
                  return;
                }

                let writablePoints = {"points":points};

                this.fs.writeFile(this.concatPaths([tmpId, "pointData", `_${newConstellationUID}.json`]), JSON.stringify(writablePoints), err => {
                  if(err){
                    callback(connectCode, secretCode, 5, "", "");
                    return;
                  }
  
                  callback(connectCode, secretCode, 1, newConstellationUID, constellationName);
                  
                });
                
              });

            });

          }
          else{
            callback(connectCode, secretCode, 2, "", "");
          }
        }
        else{
          callback(connectCode, secretCode, 6, "", "");
        }
      }

    });
  }

  getConstellationImageFromStruct(connectCode, secretCode, constellationUID, imagePath, callback){
    this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(this.imageNotFoundFile);
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            let tmpId = results[0].identifier;

            this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
              if (err){
                callback(this.imageNotFoundFile);
                return;
              }

              let {constellations} = JSON.parse(data);
              for(let i = 0; i < constellations.length; i++){
                if(constellations[i].uid == constellationUID){


                  /*SYNC CODE
                  for(let j = 0; j < constellations[i].imageData.length; j++){
                    let imageDataStr = this.fs.readFileSync(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), {encoding: "utf8", flag: "r"});
                    let imageData = JSON.parse(imageDataStr);
                    if(imageData.imagePath == imagePath){
                      callback(this.concatPaths([tmpId, "imageData", imageData.imagePath]));
                      return;
                    }
                  }
                  callback(this.imageNotFoundFile);
                  // END SYNC CODE */

                  //ASYNC FIX
                  let completionStatus = {timesRan: 0, called: 0};

                  for(let j = 0; j < constellations[i].imageData.length; j++){

                    this.fs.readFile(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), {encoding: "utf8", flag: "r"}, (err, imageDataStr) => {
                      let imageData = JSON.parse(imageDataStr);
                      if(imageData.imagePath == imagePath){
                        callback(this.concatPaths([tmpId, "imageData", imageData.imagePath]));
                        completionStatus.called++;
                      }
                      completionStatus.timesRan++;
                    })

                  }

                  this.waitForAsyncCompletion(completionStatus, constellations[i].imageData.length, this.asyncWaitDelay, () => {
                    if(completionStatus.called == 0){
                      callback(this.imageNotFoundFile);
                    }
                  });
                  //END ASYNC FIX


                }
              }

            });

          }
          else{
            callback(this.imageNotFoundFile);
          }
        }
        else{
          callback(this.imageNotFoundFile);
        }
      }

    });
  }

  getConstellationData = (connectCode, secretCode, constellationUID, dataType, callback) => {
    //connectCode, secretCode, existsInt, constellationUID, constellationName, extraData

    this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(connectCode, secretCode, 0, "", "", "", []);
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            let tmpId = results[0].identifier;

            this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
              if (err){
                callback(connectCode, secretCode, 3, "", "", "", []);
                return;
              }

              let {constellations} = JSON.parse(data);
              for(let i = 0; i < constellations.length; i++){
                if(constellations[i].uid == constellationUID){
                  if(dataType == "pointData"){

                    this.fs.readFile(this.concatPaths([tmpId, "pointData", constellations[i].pointData]), (err, pointDataStr) => {
                      if (err){
                        callback(connectCode, secretCode, 6, "", "", "", []);
                        return;
                      }
                      let {points} = JSON.parse(pointDataStr);
                      callback(connectCode, secretCode, 1, constellationUID, constellations[i].name, dataType, points);
                    });

                  }
                  else if(dataType == "imageData"){

                    /* SYNC CODE
                    let imageDataRet = [];
                    for(let j = 0; j < constellations[i].imageData.length; j++){
                      let imageDataStr = this.fs.readFileSync(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), {encoding: "utf8", flag: "r"});
                      let imageData = JSON.parse(imageDataStr);
                      imageData.sourceHolderFile = constellations[i].imageData[j];
                      imageDataRet.push(imageData);
                    }
                    callback(connectCode, secretCode, 1, constellationUID, constellations[i].name, dataType, imageDataRet);
                    // END SYNC CODE */

                    // ASYNC FIX
                    let completionStatus = {timesRan: 0};

                    let imageDataRet = [];
                    for(let j = 0; j < constellations[i].imageData.length; j++){
                      this.fs.readFile(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), {encoding: "utf8", flag: "r"}, (err, imageDataStr) => {
                        let imageData = JSON.parse(imageDataStr);
                        imageData.sourceHolderFile = constellations[i].imageData[j];
                        imageDataRet.push(imageData);
                        completionStatus.timesRan++;
                      });
                    }

                    this.waitForAsyncCompletion(completionStatus, constellations[i].imageData.length, this.asyncWaitDelay, () => {
                      callback(connectCode, secretCode, 1, constellationUID, constellations[i].name, dataType, imageDataRet);
                    });

                    // END ASYNC FIX

                  }
                  else if(dataType == "noteData"){

                    /* SYNC CODE
                    let noteDataRet = [];
                    for(let j = 0; j < constellations[i].noteData.length; j++){
                      let noteDataStr = this.fs.readFileSync(this.concatPaths([tmpId, "noteData", constellations[i].noteData[j]]), {encoding: "utf8", flag: "r"});
                      let noteData = JSON.parse(noteDataStr);
                      noteData.sourceHolderFile = constellations[i].noteData[j];
                      noteDataRet.push(noteData);
                    }
                    callback(connectCode, secretCode, 1, constellationUID, constellations[i].name, dataType, noteDataRet);
                    // END SYNC CODE */

                    // ASYNC FIX
                    let completionStatus = {timesRan: 0};

                    let noteDataRet = [];
                    for(let j = 0; j < constellations[i].noteData.length; j++){
                      this.fs.readFile(this.concatPaths([tmpId, "noteData", constellations[i].noteData[j]]), {encoding: "utf8", flag: "r"}, (err, noteDataStr) => {
                        let noteData = JSON.parse(noteDataStr);
                        noteData.sourceHolderFile = constellations[i].noteData[j];
                        noteDataRet.push(noteData);
                        completionStatus.timesRan++;
                      });
                    }

                    this.waitForAsyncCompletion(completionStatus, constellations[i].noteData.length, this.asyncWaitDelay, () => {
                      callback(connectCode, secretCode, 1, constellationUID, constellations[i].name, dataType, noteDataRet);
                    });
                    // END ASYNC FIX

                  }
                  else{
                    callback(connectCode, secretCode, 1, constellationUID, constellations[i].name, dataType, []);
                  }

                  return;
                }
              }

            });

          }
          else{
            callback(connectCode, secretCode, 2, "", "", "", []);
          }
        }
        else{
          callback(connectCode, secretCode, 0, "", "", "", []);
        }
      }

    });
  }

  removeConstellation = (connectCode, secretCode, constellationUID, callback) => {
    this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(connectCode, secretCode, 0, constellationUID);
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            let tmpId = results[0].identifier;

            this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
              if (err){
                callback(connectCode, secretCode, 3, constellationUID);
                return;
              }

              let {constellations} = JSON.parse(data);
              for(let i = 0; i < constellations.length; i++){

                if(constellations[i].uid == constellationUID){

                  this.fs.rm(this.concatPaths([tmpId, "pointData", constellations[i].pointData]), (err) => {});

                  /* SYNC CODE
                  for(let j = 0; j < constellations[i].imageData.length; j++){
                    let rawdata = this.fs.readFileSync(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]));
                    let imageData = JSON.parse(rawdata);

                    this.fs.rm(this.concatPaths([tmpId, "imageData", imageData.imagePath]), (err) => {});
                    this.fs.rm(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), (err) => {});
                  }

                  for(let j = 0; j < constellations[i].noteData.length; j++){
                    this.fs.rm(this.concatPaths([tmpId, "noteData", constellations[i].noteData[j]]), (err) => {});
                  }
                  constellations.splice(i, 1);

                  this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify({"constellations":constellations}), (err) => {
                    if(err){
                      callback(connectCode, secretCode, 5, constellationUID)
                      return;
                    }
                    callback(connectCode, secretCode, 1, constellationUID)
                    return;
                  });
                  return;

                  // END SYNC CODE */

                  // ASYNC FIX

                  let completionStatus = {timesRan: 0};

                  for(let j = 0; j < constellations[i].imageData.length; j++){
                    this.fs.readFile(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), (err, rawdata) => {
                      let imageData = JSON.parse(rawdata);

                      this.fs.rm(this.concatPaths([tmpId, "imageData", imageData.imagePath]), (err) => {});
                      this.fs.rm(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), (err) => {});

                      completionStatus.timesRan++;
                    });
                  }

                  for(let j = 0; j < constellations[i].noteData.length; j++){
                    this.fs.rm(this.concatPaths([tmpId, "noteData", constellations[i].noteData[j]]), (err) => {});
                  }

                  this.waitForAsyncCompletion(completionStatus, constellations[i].imageData.length, this.asyncWaitDelay, () => {
                    constellations.splice(i, 1);
                    this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify({"constellations":constellations}), (err) => {
                      if(err){
                        callback(connectCode, secretCode, 5, constellationUID)
                        return;
                      }
                      callback(connectCode, secretCode, 1, constellationUID)
                      return;
                    });
                  });
                  return;

                  // END ASYNC FIX

                }

              }

              callback(connectCode, secretCode, 4, constellationUID);

            });

          }
          else{
            callback(connectCode, secretCode, 2, constellationUID);
          }
        }
        else{
          callback(connectCode, secretCode, 0, constellationUID);
        }
      }

    });
  }

  isFileValid = (file, extensionWrapper) => {
    let ftype = file.originalFilename.split(".").pop();
    extensionWrapper.type = ftype;
    let validTypes = ["jpg", "jpeg", "png"];
    if(validTypes.indexOf(ftype) === -1){
      return false;
    }
    return true;
  }

  uploadImageData = (connectCode, secretCode, constellationUID, creator, callback, files) => {

    let keys = Object.keys(files);

    let file = files[keys[0]];
    let extensionWrapper = {type: ""};
    let isValid = this.isFileValid(file, extensionWrapper);
    let fileName = this.concatPathsTmp([`${this.randomString(30)}.dat`]);

    if(!isValid){
      callback(connectCode, secretCode, 2, constellationUID, creator, "");
      return;
    }
    else{
      try{
        this.fs.rename(file.filepath, fileName, (err) => {
          if(err){
            callback(connectCode, secretCode, 4, constellationUID, creator, "");
            return;
          }

          //MAYBE DO IMAGE RESIZING HERE, COMPRESSION, THEN GIVE IT ANOTHER FILENAME

          let fileToPutInData = fileName;

          this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

            if(results == undefined){
              callback(connectCode, secretCode, 5, constellationUID, creator, "");
            }
            else{
              if(results.length == 1){

                if(results[0].secretJoinCode == secretCode){
                  let tmpId = results[0].identifier;
      
                  this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
                    if (err){
                      callback(connectCode, secretCode, 6, constellationUID, creator, "");
                      return;
                    }
      
                    let {constellations} = JSON.parse(data);
                    for(let i = 0; i < constellations.length; i++){
                      if(constellations[i].uid == constellationUID){
                        let imgCustId = this.randomString(20);
                        let imageBytesSaveLoc = this.concatPaths([tmpId, "imageData", `_${imgCustId}.${extensionWrapper.type}`]);
                        let imageDescriptorSaveLoc = this.concatPaths([tmpId, "imageData", `${imgCustId}.json`]);
                        this.fs.rename(fileToPutInData, imageBytesSaveLoc, (err) => {

                          let imageData = {"creator": creator, "imagePath": `_${imgCustId}.${extensionWrapper.type}`}
                          this.fs.writeFile(imageDescriptorSaveLoc, JSON.stringify(imageData), (err) => {
                            if(err){
                              callback(connectCode, secretCode, 8, constellationUID, creator, "");
                              return;
                            }

                            constellations[i].imageData.push(`${imgCustId}.json`);
                            this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify({"constellations": constellations}), (err) => {

                              if(err){
                                callback(connectCode, secretCode, 9, constellationUID, creator, "");
                                return;
                              }
                              callback(connectCode, secretCode, 1, constellationUID, creator, `${imgCustId}.json`);
                            });
                            
                          });
                        });

                        return;
                      }
                    }
      
                    callback(connectCode, secretCode, 7, constellationUID, creator, "");
      
                  });
      
                }
                else{
                  callback(connectCode, secretCode, 8, constellationUID, creator, "");
                }
              }
              else{
                callback(connectCode, secretCode, 9, constellationUID, creator, "");
              }
            }
      
          });

        })
      }
      catch(err){
        callback(connectCode, secretCode, 3, constellationUID, creator, "");
      }
    }

  }

  deleteImageData = (connectCode, secretCode, constellationUID, sourceHolderFile, callback) => {
    //connectcode, secretcode, existsint

    this.con.query('SELECT name, secretJoinCode, creator, identifier FROM galaxies WHERE clientConnectCode=?', [connectCode], (error, results, fields) => {

      if(results == undefined){
        callback(connectCode, secretCode, 0, constellationUID);
      }
      else{
        if(results.length == 1){
          if(results[0].secretJoinCode == secretCode){
            let tmpId = results[0].identifier;

            this.fs.readFile(this.concatPaths([tmpId, "linker.json"]), (err, data) => {
              if (err){
                callback(connectCode, secretCode, 3, constellationUID);
                return;
              }

              let {constellations} = JSON.parse(data);
              for(let i = 0; i < constellations.length; i++){

                if(constellations[i].uid == constellationUID){
                  for(let j = 0; j < constellations[i].imageData.length; j++){

                    if(constellations[i].imageData[j] == sourceHolderFile){
                      this.fs.readFile(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), (err2, data2) => {
                        if(err2){
                          callback(connectCode, secretCode, 6, constellationUID);
                        }
                        let imageDataSourceFileStruct = JSON.parse(data2);
                        let actualImagePath = this.concatPaths([tmpId, "imageData", imageDataSourceFileStruct.imagePath]);

                        this.fs.rm(actualImagePath, (err) => {});
                        this.fs.rm(this.concatPaths([tmpId, "imageData", constellations[i].imageData[j]]), (err) => {});
                        constellations[i].imageData.splice(j, 1);
                        this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify({"constellations": constellations}), (err) => {});

                        callback(connectCode, secretCode, 1, constellationUID);
                      });

                      return;
                    }

                  }
                  callback(connectCode, secretCode, 5, constellationUID);

                }

              }

              callback(connectCode, secretCode, 4, constellationUID);

            });

          }
          else{
            callback(connectCode, secretCode, 2, constellationUID);
          }
        }
        else{
          callback(connectCode, secretCode, 0, constellationUID);
        }
      }

    });
  }

  uploadNoteData = (connectCode, secretCode, constellationUID, creator, noteText, callback) => {
    this.accessGalaxyAndReadLinker(connectCode, secretCode, (wasError, constellations, tmpId) => {
      if(wasError == 0){

        for(let i = 0; i < constellations.length; i++){
          if(constellations[i].uid == constellationUID){
            let noteDataId = this.randomString(20);

            this.fs.writeFile(this.concatPaths([tmpId, "noteData", `${noteDataId}.json`]), JSON.stringify({"creator":creator, "text":noteText}), (err) => {
              if(err){
                callback(connectCode, secretCode, 11, constellationUID, creator, noteText, "");
                return;
              }
              
              constellations[i].noteData.push(`${noteDataId}.json`);
              this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify({"constellations":constellations}), (err) => {
                if(err){
                  callback(connectCode, secretCode, 12, constellationUID, creator, noteText, "");
                  return;
                }
                callback(connectCode, secretCode, 1, constellationUID, creator, noteText, `${noteDataId}.json`);
              });
            });

            return;
          }
        }

        callback(connectCode, secretCode, 10, constellationUID, creator, noteText, "");
      }
      else{
        callback(connectCode, secretCode, wasError, constellationUID, creator, noteText, "");
      }
    });
  }

  deleteNoteData = (connectCode, secretCode, constellationUID, sourceHolderFile, callback) => {
    //connectcode, secretcode, existint
    this.accessGalaxyAndReadLinker(connectCode, secretCode, (wasError, constellations, tmpId) => {

      if(wasError == 0){
        for(let i = 0; i < constellations.length; i++){

          if(constellations[i].uid == constellationUID){
            for(let j = 0; j < constellations[i].noteData.length; j++){

              if(constellations[i].noteData[j] == sourceHolderFile){
                this.fs.rm(this.concatPaths([tmpId, "noteData", constellations[i].noteData[j]]), (err) => {});
                constellations[i].noteData.splice(j, 1);
                this.fs.writeFile(this.concatPaths([tmpId, "linker.json"]), JSON.stringify({"constellations":constellations}), (err) => {});

                callback(connectCode, secretCode, 1, constellationUID);

                return;
              }

            }
            callback(connectCode, secretCode, 5, constellationUID);

          }
        }
        callback(connectCode, secretCode, 10, constellationUID, []);
      }
      else{
        callback(connectCode, secretCode, wasError, constellationUID);
      }
      
    });
    

  }

  editPointData = (connectCode, secretCode, constellationUID, points, callback) => {

    this.accessGalaxyAndReadLinker(connectCode, secretCode, (wasError, constellations, tmpId) => {

      if(wasError == 0){
        for(let i = 0; i < constellations.length; i++){

          if(constellations[i].uid == constellationUID){
            let pointDataPath = this.concatPaths([tmpId, "pointData", constellations[i].pointData]);
            this.fs.writeFile(pointDataPath, JSON.stringify({"points": points}), (err) => {
              if(err){   
                callback(connectCode, secretCode, 11, constellationUID, points);
                return;
              }
              callback(connectCode, secretCode, 1, constellationUID, points);
            })

            return;
          }
        }
        callback(connectCode, secretCode, 10, constellationUID, points);
      }
      else{
        callback(connectCode, secretCode, wasError, constellationUID, points);
      }
      
    });

  }

}