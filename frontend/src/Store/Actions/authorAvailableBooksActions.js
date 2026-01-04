import { createAction } from 'redux-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { fetchBooks } from './bookActions';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createClearReducer from './Creators/Reducers/createClearReducer';

//
// Variables

export const section = 'authorAvailableBooks';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isAdding: false,
  addError: null,
  isExcluding: false,
  excludeError: null,
  items: [],
  authorId: null
};

//
// Actions Types

export const FETCH_AUTHOR_AVAILABLE_BOOKS = 'authorAvailableBooks/fetchAuthorAvailableBooks';
export const ADD_AUTHOR_BOOKS = 'authorAvailableBooks/addAuthorBooks';
export const EXCLUDE_AUTHOR_AVAILABLE_BOOKS = 'authorAvailableBooks/excludeAuthorAvailableBooks';
export const CLEAR_AUTHOR_AVAILABLE_BOOKS = 'authorAvailableBooks/clearAuthorAvailableBooks';

//
// Action Creators

export const fetchAuthorAvailableBooks = createThunk(FETCH_AUTHOR_AVAILABLE_BOOKS);
export const addAuthorBooks = createThunk(ADD_AUTHOR_BOOKS);
export const excludeAuthorAvailableBooks = createThunk(EXCLUDE_AUTHOR_AVAILABLE_BOOKS);
export const clearAuthorAvailableBooks = createAction(CLEAR_AUTHOR_AVAILABLE_BOOKS);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_AUTHOR_AVAILABLE_BOOKS]: function(getState, payload, dispatch) {
    const { authorId } = payload;

    dispatch(set({
      section,
      isFetching: true,
      error: null,
      authorId
    }));

    const { request, abortRequest } = createAjaxRequest({
      url: `/author/${authorId}/books`
    });

    request.done((data) => {
      dispatch(update({ section, data }));

      dispatch(set({
        section,
        isFetching: false,
        isPopulated: true,
        error: null,
        authorId
      }));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  },

  [ADD_AUTHOR_BOOKS]: function(getState, payload, dispatch) {
    const { authorId, foreignBookIds } = payload;

    dispatch(set({
      section,
      isAdding: true,
      addError: null
    }));

    const { request } = createAjaxRequest({
      url: `/author/${authorId}/books`,
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify({
        foreignBookIds
      })
    });

    request.done(() => {
      dispatch(set({
        section,
        isAdding: false,
        addError: null
      }));

      dispatch(fetchBooks({ authorId }));
      dispatch(fetchAuthorAvailableBooks({ authorId }));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isAdding: false,
        addError: xhr
      }));
    });
  },

  [EXCLUDE_AUTHOR_AVAILABLE_BOOKS]: function(getState, payload, dispatch) {
    const { authorId, foreignBookIds } = payload;

    dispatch(set({
      section,
      isExcluding: true,
      excludeError: null
    }));

    const { request } = createAjaxRequest({
      url: `/author/${authorId}/books/exclude`,
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify({
        foreignBookIds
      })
    });

    request.done(() => {
      dispatch(set({
        section,
        isExcluding: false,
        excludeError: null
      }));

      dispatch(fetchAuthorAvailableBooks({ authorId }));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isExcluding: false,
        excludeError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({
  [CLEAR_AUTHOR_AVAILABLE_BOOKS]: createClearReducer(section, {
    isFetching: false,
    isPopulated: false,
    error: null,
    isAdding: false,
    addError: null,
    isExcluding: false,
    excludeError: null,
    items: [],
    authorId: null
  })
}, defaultState, section);
